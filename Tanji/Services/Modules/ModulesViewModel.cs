using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Net.Sockets;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

using Tanji.Helpers;
using Tanji.Services.Modules.Models;

using Sulakore.Habbo;
using Sulakore.Modules;
using Sulakore.Network;
using Sulakore.Network.Protocol;

namespace Tanji.Services.Modules
{
    public class ModulesViewModel : ObservableObject, IReceiver, IHaltable, ISynchronizer
    {
        private ModuleInfo[] _safeModules;

        private readonly List<string> _hashBlacklist;
        private readonly OpenFileDialog _installModuleDialog;
        private static readonly Dictionary<string, ModuleInfo> _moduleCache;

        public Command InstallCommand { get; }
        public Command UninstallCommand { get; }

        public DirectoryInfo ModulesDirectory { get; }
        public DirectoryInfo DependenciesDirectory { get; }
        public ObservableCollection<ModuleInfo> Modules { get; }

        private ModuleInfo _selectedModule;
        public ModuleInfo SelectedModule
        {
            get => _selectedModule;
            set
            {
                _selectedModule = value;
                RaiseOnPropertyChanged();
            }
        }

        static ModulesViewModel()
        {
            _moduleCache = new Dictionary<string, ModuleInfo>();
        }
        public ModulesViewModel()
        {
            _safeModules = new ModuleInfo[0];
            _hashBlacklist = new List<string>();

            _installModuleDialog = new OpenFileDialog();
            _installModuleDialog.Title = "Tanji - Install Module";
            _installModuleDialog.Filter = ".NET Assembly (*.dll, *.exe)|*.dll; *.exe|Dynamic Link Library (*.dll)|*.dll|Executable (*.exe)|*.exe";

            AppDomain.CurrentDomain.AssemblyResolve += Assembly_Resolve;

            InstallCommand = new Command(Install);
            UninstallCommand = new Command(Uninstall, CanUninstall);

            Modules = new ObservableCollection<ModuleInfo>();
            Modules.CollectionChanged += Modules_CollectionChanged;

            if (App.Master != null)
            {
                ModulesDirectory = Directory.CreateDirectory("Installed Modules");
                DependenciesDirectory = ModulesDirectory.CreateSubdirectory("Dependencies");
                LoadModules();
                try
                {
                    var listener = new TcpListener(IPAddress.Any, TService.REMOTE_MODULE_PORT);
                    listener.Start();

                    Task captureModulesTask = CaptureModulesAsync(listener);
                }
                catch { App.Display(null, $"Failed to start module listener on port '{TService.REMOTE_MODULE_PORT}'."); }
            }
        }

        private async Task HandleModuleDataAsync(ModuleInfo module)
        {
            try
            {
                while (module.Node.IsConnected)
                {
                    HPacket packet = await module.Node.ReceivePacketAsync().ConfigureAwait(false);
                    switch (packet.Id)
                    {
                        case 1:
                        {
                            string identifier = packet.ReadUTF8();
                            TaskCompletionSource<HPacket> handledDataSource = null;
                            if (module.DataAwaiters.TryGetValue(identifier, out handledDataSource))
                            {
                                handledDataSource.SetResult(packet);
                            }
                            break;
                        }
                        case 2:
                        {
                            byte[] packetData = packet.ReadBytes(packet.ReadInt32(1), 5);
                            if (packet.ReadBoolean()) // IsOutgoing
                            {
                                await App.Master.Connection.SendToServerAsync(packetData).ConfigureAwait(false);
                            }
                            else
                            {
                                await App.Master.Connection.SendToClientAsync(packetData).ConfigureAwait(false);
                            }
                            break;
                        }
                    }
                }
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    module.Dispose();
                    Modules.Remove(module);
                });
            }
        }
        private async Task CaptureModulesAsync(TcpListener listener)
        {
            try
            {
                var moduleNode = new HNode(await listener.AcceptSocketAsync());

                moduleNode.InFormat = HFormat.EvaWire;
                moduleNode.OutFormat = HFormat.EvaWire;
                HPacket infoPacket = await moduleNode.ReceivePacketAsync();

                var module = new ModuleInfo(moduleNode);
                module.PropertyChanged += Module_PropertyChanged;

                module.Version = Version.Parse(infoPacket.ReadUTF8());
                module.Name = infoPacket.ReadUTF8();
                module.Description = infoPacket.ReadUTF8();

                module.Authors.Capacity = infoPacket.ReadInt32();
                for (int i = 0; i < module.Authors.Capacity; i++)
                {
                    module.Authors.Add(new AuthorAttribute(infoPacket.ReadUTF8()));
                }
                Modules.Add(module);

                module.Initialize();
                Task handleModuleDataTask = HandleModuleDataAsync(module);
            }
            finally { Task captureModulesAsync = CaptureModulesAsync(listener); }
        }

        private void Install(object obj)
        {
            var modulePath = (obj as string);
            _installModuleDialog.FileName = string.Empty;
            if (string.IsNullOrWhiteSpace(modulePath) &&
                _installModuleDialog.ShowDialog() != DialogResult.Cancel)
            {
                modulePath = _installModuleDialog.FileName;
            }
            if (string.IsNullOrWhiteSpace(modulePath)) return;

            // Check if the file was blacklisted based on its MD5 hash, if so, do not attempt to install.
            string hash = GetFileHash(modulePath);
            if (_hashBlacklist.Contains(hash)) return;

            // Check if this module is already installed.
            ModuleInfo module = GetModule(hash);
            if (module != null)
            {
                SelectedModule = module;
                module.FormUI?.BringToFront();
                return;
            }

            // Do not remove from, or empty the module cache.
            // There may be a case where a previously uninstalled module will be be reinstalled in the same session.
            if (!_moduleCache.TryGetValue(hash, out module))
            {
                // Load it through memory, do not feed a local file path/stream(don't want to lock the file).
                module = new ModuleInfo();
                module.Assembly = Assembly.Load(File.ReadAllBytes(modulePath));

                module.Hash = hash;
                module.PropertyChanged += Module_PropertyChanged;

                // Copy the required dependencies, since utilizing 'ExportedTypes' will attempt to load them when enumerating.
                CopyDependencies(modulePath, module.Assembly);
                try
                {
                    foreach (Type type in module.Assembly.ExportedTypes)
                    {
                        if (!typeof(IModule).IsAssignableFrom(type)) continue;

                        var moduleAtt = type.GetCustomAttribute<ModuleAttribute>();
                        if (moduleAtt == null) continue;

                        module.Type = type;
                        module.Name = moduleAtt.Name;
                        module.EntryType = moduleAtt.EntryType;
                        module.Description = moduleAtt.Description;
                        module.PropertyName = moduleAtt.PropertyName;
                        module.Version = module.Assembly.GetName().Version;

                        var authorAtts = type.GetCustomAttributes<AuthorAttribute>();
                        module.Authors.AddRange(authorAtts);

                        // Only add it to the cache if this is a valid module.
                        _moduleCache.Add(hash, module);
                        break;
                    }
                    if (module.Type == null) return;
                }
                finally
                {
                    if (module.Type == null)
                    {
                        _hashBlacklist.Add(module.Hash);
                    }
                }
            }

            string installPath = CopyFile(modulePath, hash);
            module.Path = installPath; // This property already might have been set from a previous installation, but it wouldn't hurt to re-set the value.
            Modules.Add(module);
        }
        private void Uninstall(object obj)
        {
            if (File.Exists(SelectedModule.Path))
            {
                File.Delete(SelectedModule.Path);
            }
            SelectedModule.Dispose();
            Modules.Remove(SelectedModule);

            SelectedModule = null;
        }
        private bool CanUninstall(object obj)
        {
            if (SelectedModule == null) return false;
            if (SelectedModule.Instance?.IsStandalone ?? false) return false;

            return true;
        }

        public ModuleInfo GetModule(string hash)
        {
            return Modules.SingleOrDefault(m => m.Hash == hash);
        }
        public IEnumerable<ModuleInfo> GetInitializedModules()
        {
            return Modules.Where(m => m.IsInitialized);
        }

        private void LoadModules()
        {
            foreach (FileSystemInfo fileSysInfo in ModulesDirectory.EnumerateFiles("*.*"))
            {
                string extension = fileSysInfo.Extension.ToLower();
                if (extension == ".exe" || extension == ".dll")
                {
                    try { Install(fileSysInfo.FullName); }
                    catch (Exception ex)
                    {
                        App.Display(ex, "Failed to install the assembly as a module.\r\nFile: " + fileSysInfo.Name);
                    }
                }
            }
        }
        private string GetFileHash(string path)
        {
            using (var md5 = MD5.Create())
            using (var fileStream = File.OpenRead(path))
            {
                return BitConverter.ToString(md5.ComputeHash(fileStream))
                    .Replace("-", string.Empty).ToLower();
            }
        }
        private string CopyFile(string path, string uniqueId)
        {
            path = Path.GetFullPath(path);
            string fileExt = Path.GetExtension(path);
            string fileName = Path.GetFileNameWithoutExtension(path);

            string copiedFilePath = path;
            string fileNameSuffix = $"({uniqueId}){fileExt}";
            if (!path.EndsWith(fileNameSuffix))
            {
                copiedFilePath = Path.Combine(ModulesDirectory.FullName, fileName + fileNameSuffix);
                if (!File.Exists(copiedFilePath))
                {
                    File.Copy(path, copiedFilePath, true);
                }
            }
            return copiedFilePath;
        }
        private void CopyDependencies(string path, Assembly assembly)
        {
            AssemblyName[] references = assembly.GetReferencedAssemblies();
            var fileReferences = new Dictionary<string, AssemblyName>(references.Length);
            foreach (AssemblyName reference in references)
            {
                fileReferences[reference.Name] = reference;
            }

            string[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetName().Name)
                .ToArray();

            var sourceDirectory = new DirectoryInfo(Path.GetDirectoryName(path));
            IEnumerable<string> missingAssemblies = fileReferences.Keys.Except(loadedAssemblies);
            foreach (string missingAssembly in missingAssemblies)
            {
                string assemblyName = fileReferences[missingAssembly].FullName;
                FileSystemInfo dependencyFile = GetDependencyFile(DependenciesDirectory, assemblyName);
                if (dependencyFile == null)
                {
                    dependencyFile = GetDependencyFile(sourceDirectory, assemblyName);
                    if (dependencyFile != null)
                    {
                        string installDependencyPath = Path.Combine(
                           DependenciesDirectory.FullName, dependencyFile.Name);

                        File.Copy(dependencyFile.FullName, installDependencyPath, true);
                    }
                }
            }
        }
        private FileSystemInfo GetDependencyFile(DirectoryInfo directory, string dependencyName)
        {
            FileSystemInfo[] libraries = directory.GetFileSystemInfos("*.dll");
            foreach (FileSystemInfo library in libraries)
            {
                string libraryName = AssemblyName.GetAssemblyName(library.FullName).FullName;
                if (libraryName == dependencyName)
                {
                    return library;
                }
            }
            return null;
        }

        private Assembly Assembly_Resolve(object sender, ResolveEventArgs e)
        {
            FileSystemInfo dependencyFile = GetDependencyFile(DependenciesDirectory, e.Name);
            if (dependencyFile != null)
            {
                return Assembly.Load(File.ReadAllBytes(dependencyFile.FullName));
            }
            return null;
        }
        private void Module_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ModuleInfo.Instance):
                {
                    IsReceiving = (GetInitializedModules().Count() > 0);
                    break;
                }
            }
        }
        private void Modules_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _safeModules = Modules.ToArray();
        }

        #region IReceiver Implementation
        public bool IsReceiving { get; private set; }
        public void HandleOutgoing(DataInterceptedEventArgs e)
        {
            if (_safeModules.Length == 0) return;
            foreach (ModuleInfo module in _safeModules)
            {
                if (module.Instance != null)
                {
                    try { module.Instance?.HandleOutgoing(e); }
                    catch (Exception ex)
                    {
                        e.Restore();
                        Task.Factory.StartNew(() => App.Display(ex));
                    }
                }
            }
        }
        public void HandleIncoming(DataInterceptedEventArgs e)
        {
            if (_safeModules.Length == 0) return;
            foreach (ModuleInfo module in _safeModules)
            {
                if (module.Instance != null)
                {
                    try { module.Instance?.HandleIncoming(e); }
                    catch (Exception ex)
                    {
                        e.Restore();
                        Task.Factory.StartNew(() => App.Display(ex));
                    }
                }
            }
        }
        #endregion
        #region IHaltable Implementation
        public void Halt()
        { }
        public void Restore()
        { }
        #endregion
        #region ISynchronizer Implementation
        public void Synchronize(HGame game)
        {
            foreach (ModuleInfo module in GetInitializedModules())
            {
                module.Instance.Synchronize(game);
            }
        }
        public void Synchronize(HGameData gameData)
        {
            foreach (ModuleInfo module in GetInitializedModules())
            {
                module.Instance.Synchronize(gameData);
            }
        }
        #endregion
    }
}