using System;
using System.Windows;
using System.Windows.Threading;
using System.Collections.Generic;

using Tanji.Network;
using Tanji.Services;
using Tanji.Windows.Logger;
using Tanji.Services.Modules;
using Tanji.Services.Connection;

using Sulakore.Habbo;
using Sulakore.Modules;
using Sulakore.Network;
using Sulakore.Habbo.Messages;

using Eavesdrop;

namespace Tanji
{
    public partial class App : Application, IMaster
    {
        private readonly List<IHaltable> _haltables;
        private readonly List<ISynchronizer> _synchronizers;
        private readonly SortedList<int, IReceiver> _receivers;

        public Incoming In { get; }
        public Outgoing Out { get; }

        IHConnection IInstaller.Connection => Connection;
        public HConnection Connection { get; }

        public HGame Game { get; set; }
        public HGameData GameData { get; set; }

        public static IMaster Master { get; private set; }

        public App()
        {
            Eavesdropper.Overrides.AddRange(new[]
            {
                "*google*",
                "*discordapp.com",
                "*gstatic.com",
                "*imgur.com",
                "*github.com",
                "*googleapis.com",
                "*facebook.com",
                "*cloudfront.net",
                "*gvt1.com",
                "*jquery.com",
                "*akamai.net",
                "*ultra-rv.com"
            });

            _haltables = new List<IHaltable>();
            _synchronizers = new List<ISynchronizer>();
            _receivers = new SortedList<int, IReceiver>();

            In = new Incoming();
            Out = new Outgoing();
            GameData = new HGameData();
            Connection = new HConnection();

            Connection.Connected += Connected;
            Connection.DataOutgoing += HandleData;
            Connection.DataIncoming += HandleData;
            Connection.Disconnected += Disconnected;

            DispatcherUnhandledException += UnhandledException;
        }

        public void AddHaltable(IHaltable haltable)
        {
            _haltables.Add(haltable);
        }
        public void AddReceiver(IReceiver receiver)
        {
            int rank = -1;
            switch (receiver.GetType().Name)
            {
                case nameof(ModulesViewModel): rank = 0; break;
                case nameof(ConnectionViewModel): rank = 1; break;

                case nameof(LoggerViewModel): rank = 10; break;

                default:
                throw new ArgumentException("Unrecognized receiver: ", nameof(receiver));
            }
            _receivers.Add(rank, receiver);
        }
        public void AddSynchronizer(ISynchronizer synchronizer)
        {
            _synchronizers.Add(synchronizer);
        }

        public void Synchronize(HGame game)
        {
            In.Load(game, "Hashes.ini");
            Out.Load(game, "Hashes.ini");
            foreach (ISynchronizer synchronizer in _synchronizers)
            {
                synchronizer.Synchronize(game);
            }
        }
        public void Synchronize(HGameData gameData)
        {
            foreach (ISynchronizer synchronizer in _synchronizers)
            {
                synchronizer.Synchronize(gameData);
            }
        }

        private void Connected(object sender, EventArgs e)
        {
            foreach (IHaltable haltable in _haltables)
            {
                haltable.Dispatcher.Invoke(haltable.Restore);
            }
        }
        private void Disconnected(object sender, EventArgs e)
        {
            foreach (IHaltable haltable in _haltables)
            {
                haltable.Dispatcher.Invoke(haltable.Halt);
            }

            Game.Dispose();
            Game = null;
        }
        private void HandleData(object sender, DataInterceptedEventArgs e)
        {
            foreach (IReceiver receiver in _receivers.Values)
            {
                if (!receiver.IsReceiving) continue;
                if (e.IsOutgoing)
                {
                    receiver.HandleOutgoing(e);
                }
                else
                {
                    receiver.HandleIncoming(e);
                }
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Master = this;
            base.OnStartup(e);
        }
        private void UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (Master != null && !e.Handled)
            {
                Display(e.Exception);
                Eavesdropper.Terminate();
            }
        }

        public static void DoEvents()
        {
            Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }
        public static void Display(Exception exception, string header = null)
        {
            string messsage = header;
            if (!string.IsNullOrWhiteSpace(messsage) && exception != null)
            {
                messsage += "\r\n\r\nException: ";
            }
            MessageBox.Show((messsage + (exception?.ToString())), "Tanji - Error!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}