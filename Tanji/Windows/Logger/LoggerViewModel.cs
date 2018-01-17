using System;
using System.Drawing;
using System.Windows;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms.Integration;

using Tanji.Helpers;
using Tanji.Services;
using Tanji.Controls;

using Sulakore.Habbo;
using Sulakore.Network;
using Sulakore.Habbo.Messages;
using Sulakore.Network.Protocol;

namespace Tanji.Windows.Logger
{
    public class LoggerViewModel : ObservableObject, IHaltable, IReceiver
    {
        private Task _dequeueTask;
        private DateTime _latencyTestStart;
        private readonly object _enqueueLock;
        private readonly object _dequeueLock;
        private readonly HPacketLogger _packetLogger;
        private readonly Queue<DataInterceptedEventArgs> _intercepted;
        private readonly Dictionary<int, MessageItem> _ignoredMessages;
        private readonly Action<List<Tuple<string, Color>>> _displayEntry;

        public Color FilterHighlight { get; set; } = Color.Yellow;
        public Color DetailHighlight { get; set; } = Color.DarkGray;
        public Color IncomingHighlight { get; set; } = Color.FromArgb(178, 34, 34);
        public Color OutgoingHighlight { get; set; } = Color.FromArgb(0, 102, 204);
        public Color StructureHighlight { get; set; } = Color.FromArgb(0, 204, 136);

        private bool _isReceiving = true;
        public bool IsReceiving
        {
            get => (_isReceiving && (IsViewingOutgoing || IsViewingIncoming));
            set => _isReceiving = value;
        }

        private WindowsFormsHost _packetLoggerHost = new WindowsFormsHost();
        public WindowsFormsHost PacketLoggerHost
        {
            get
            {
                if (_packetLoggerHost.Child == null)
                {
                    _packetLoggerHost.Child = _packetLogger;
                }
                return _packetLoggerHost;
            }
        }

        private Visibility _visibility = Visibility.Collapsed;
        public Visibility Visibility
        {
            get
            {
                if (App.Master == null)
                {
                    return Visibility.Visible;
                }
                return _visibility;
            }
            set
            {
                _visibility = value;
                RaiseOnPropertyChanged();
            }
        }

        private bool _isDisplayingBlocked = true;
        public bool IsDisplayingBlocked
        {
            get => _isDisplayingBlocked;
            set
            {
                _isDisplayingBlocked = value;
                RaiseOnPropertyChanged();
            }
        }

        private bool _isDisplayingReplaced = true;
        public bool IsDisplayingReplaced
        {
            get => _isDisplayingReplaced;
            set
            {
                _isDisplayingReplaced = value;
                RaiseOnPropertyChanged();
            }
        }

        private bool _isDisplayingHash = true;
        public bool IsDisplayingHash
        {
            get
            {
                if (App.Master?.Game?.IsPostShuffle ?? true)
                {
                    return _isDisplayingHash;
                }
                return false;
            }
            set
            {
                _isDisplayingHash = value;
                RaiseOnPropertyChanged();
            }
        }

        private bool _isDisplayingHexadecimal = false;
        public bool IsDisplayingHexadecimal
        {
            get => _isDisplayingHexadecimal;
            set
            {
                _isDisplayingHexadecimal = value;
                RaiseOnPropertyChanged();
            }
        }

        private bool _isDisplayingStructure = true;
        public bool IsDisplayingStructure
        {
            get => _isDisplayingStructure;
            set
            {
                _isDisplayingStructure = value;
                RaiseOnPropertyChanged();
            }
        }

        private bool _isDisplayingTimestamp = false;
        public bool IsDisplayingTimestamp
        {
            get => _isDisplayingTimestamp;
            set
            {
                _isDisplayingTimestamp = value;
                RaiseOnPropertyChanged();
            }
        }

        private bool _isDisplayingParserName = true;
        public bool IsDisplayingParserName
        {
            get => _isDisplayingParserName;
            set
            {
                _isDisplayingParserName = value;
                RaiseOnPropertyChanged();
            }
        }

        private bool _isDisplayingMessageName = true;
        public bool IsDisplayingMessageName
        {
            get => _isDisplayingMessageName;
            set
            {
                _isDisplayingMessageName = value;
                RaiseOnPropertyChanged();
            }
        }

        private bool _isViewingOutgoing = true;
        public bool IsViewingOutgoing
        {
            get => _isViewingOutgoing;
            set
            {
                _isViewingOutgoing = value;
                RaiseOnPropertyChanged();
            }
        }

        private bool _isViewingIncoming = true;
        public bool IsViewingIncoming
        {
            get => _isViewingIncoming;
            set
            {
                _isViewingIncoming = value;
                RaiseOnPropertyChanged();
            }
        }

        private int _latency = 0;
        public int Latency
        {
            get => _latency;
            set
            {
                _latency = value;
                RaiseOnPropertyChanged();
            }
        }

        private bool _isAlwaysOnTop = false;
        public bool IsAlwaysOnTop
        {
            get => _isAlwaysOnTop;
            set
            {
                _isAlwaysOnTop = value;
                if (Application.Current?.MainWindow != null)
                {
                    Application.Current.MainWindow.Topmost = value;
                }
                RaiseOnPropertyChanged();
            }
        }

        public string Revision => (App.Master?.Game?.Revision ?? "?");

        public Command FindCommand { get; }
        public Command IgnoreCommand { get; }
        public Command EmptyLogCommand { get; }

        public Command ToggleAlwaysOnTopCommand { get; }
        public Command ToggleViewOutgoingCommand { get; }
        public Command ToggleViewIncomingCommand { get; }

        public LoggerViewModel()
        {
            _displayEntry = DisplayEntries;
            _enqueueLock = new object();
            _dequeueLock = new object();
            _packetLogger = new HPacketLogger();
            _intercepted = new Queue<DataInterceptedEventArgs>();
            _ignoredMessages = new Dictionary<int, MessageItem>();

            FindCommand = new Command(Find);
            IgnoreCommand = new Command(Ignore);
            EmptyLogCommand = new Command(EmptyLog);
            ToggleAlwaysOnTopCommand = new Command(ToggleAlwaysOnTop);
            ToggleViewOutgoingCommand = new Command(ToggleViewOutgoing);
            ToggleViewIncomingCommand = new Command(ToggleViewIncoming);
        }

        public void LoggerActivated(object sender, EventArgs e)
        {
            IsReceiving = true;
        }
        public void LoggerClosing(object sender, CancelEventArgs e)
        {
            IsReceiving = false;
            _packetLogger.LoggerTxt.Clear();

            _intercepted.Clear();
        }

        private void Find(object obj)
        { }
        private void Ignore(object obj)
        { }
        private void EmptyLog(object obj)
        {
            _packetLogger.LoggerTxt.Clear();
        }

        private void ToggleAlwaysOnTop(object obj)
        {
            IsAlwaysOnTop = !IsAlwaysOnTop;
        }
        private void ToggleViewIncoming(object obj)
        {
            IsViewingIncoming = !IsViewingIncoming;
        }
        private void ToggleViewOutgoing(object obj)
        {
            IsViewingOutgoing = !IsViewingOutgoing;
        }

        public void Halt()
        {
            IsReceiving = false;
            Visibility = Visibility.Collapsed;

            _intercepted.Clear();
            _packetLogger.LoggerTxt.Clear();
        }
        public void Restore()
        {
            _packetLogger.LoggerTxt.Clear();

            Visibility = Visibility.Visible;
            IsReceiving = true;

            RaiseOnPropertyChanged(nameof(Revision));
        }

        public void HandleOutgoing(DataInterceptedEventArgs e)
        {
            if (e.Packet.Id == App.Master.Out.LatencyTest)
            {
                _latencyTestStart = e.Timestamp;
            }
            PushToQueue(e);
        }
        public void HandleIncoming(DataInterceptedEventArgs e)
        {
            if (e.Packet.Id == App.Master.In.LatencyResponse)
            {
                Latency = (int)(e.Timestamp - _latencyTestStart).TotalMilliseconds;
            }
            PushToQueue(e);
        }

        private void PopFromQueue()
        {
            if (Monitor.TryEnter(_dequeueLock))
            {
                var entered = DateTime.Now;
                while (!_packetLogger.IsHandleCreated)
                {
                    Thread.Sleep(10);
                }
                try
                {
                    var start = DateTime.Now;
                    var entries = new List<Tuple<string, Color>>();
                    while (IsReceiving)
                    {
                        while (_intercepted.Count == 0)
                        {
                            Thread.Sleep(100);
                        }

                        AddEntry(entries);
                        if ((DateTime.Now - start).TotalMilliseconds >= 500)
                        {
                            _packetLogger.Invoke(_displayEntry, entries);
                            entries.Clear();

                            App.DoEvents();
                            Thread.Sleep(10);
                        }
                    }
                }
                finally
                {
                    Monitor.Exit(_dequeueLock);
                    App.DoEvents();
                }
            }
        }
        private void PushToQueue(DataInterceptedEventArgs args)
        {
            lock (_enqueueLock)
            {
                if (IsLoggingAuthorized(args))
                {
                    _intercepted.Enqueue(args);
                    if (_dequeueTask?.IsCompleted ?? true)
                    {
                        _dequeueTask = Task.Factory.StartNew(
                            PopFromQueue, TaskCreationOptions.LongRunning);
                    }
                }
            }
        }

        private void AddEntry(List<Tuple<string, Color>> entries)
        {
            DataInterceptedEventArgs args = _intercepted.Dequeue();
            if (!IsLoggingAuthorized(args)) return;

            if (args.IsBlocked)
            {
                entries.Add(Tuple.Create("[Blocked]\r\n", FilterHighlight));
            }
            if (!args.IsOriginal)
            {
                entries.Add(Tuple.Create("[Replaced]\r\n", FilterHighlight));
            }
            if (IsDisplayingTimestamp)
            {
                entries.Add(Tuple.Create($"[{args.Timestamp:M/d H:mm:ss}]\r\n", DetailHighlight));
            }

            MessageItem message = GetMessage(args);
            if (IsDisplayingHash && message != null && !string.IsNullOrWhiteSpace(message.Hash))
            {
                var identifiers = (args.IsOutgoing ? (Identifiers)App.Master.Out : App.Master.In);

                string name = identifiers.GetName(message.Hash);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    entries.Add(Tuple.Create($"[{name}] ", DetailHighlight));
                }
                entries.Add(Tuple.Create($"[{message.Hash}]\r\n", DetailHighlight));
            }

            if (IsDisplayingHexadecimal)
            {
                string hex = BitConverter.ToString(args.Packet.ToBytes());
                entries.Add(Tuple.Create($"[{hex.Replace("-", string.Empty)}]\r\n", DetailHighlight));
            }

            string arrow = "->";
            string title = "Outgoing";
            Color entryHighlight = OutgoingHighlight;
            if (!args.IsOutgoing)
            {
                arrow = "<-";
                title = "Incoming";
                entryHighlight = IncomingHighlight;
            }

            entries.Add(Tuple.Create(title + "[", entryHighlight));
            entries.Add(Tuple.Create(args.Packet.Id.ToString(), DetailHighlight));

            if (message != null)
            {
                if (IsDisplayingMessageName)
                {
                    entries.Add(Tuple.Create(", ", entryHighlight));
                    entries.Add(Tuple.Create(message.Class.QName.Name, DetailHighlight));
                }
                if (IsDisplayingParserName && message.Parser != null)
                {
                    entries.Add(Tuple.Create(", ", entryHighlight));
                    entries.Add(Tuple.Create(message.Parser.QName.Name, DetailHighlight));
                }
            }
            entries.Add(Tuple.Create("]", entryHighlight));
            entries.Add(Tuple.Create($" {arrow} ", DetailHighlight));
            entries.Add(Tuple.Create($"{args.Packet}\r\n", entryHighlight));

            if (IsDisplayingStructure && message?.Structure?.Length >= 0)
            {
                int position = 0;
                HPacket packet = args.Packet;
                string structure = ("{id:" + packet.Id + "}");
                foreach (string valueType in message.Structure)
                {
                    switch (valueType.ToLower())
                    {
                        case "int":
                        structure += ("{i:" + packet.ReadInt32(ref position) + "}");
                        break;

                        case "string":
                        structure += ("{s:" + packet.ReadUTF8(ref position) + "}");
                        break;

                        case "double":
                        structure += ("{d:" + packet.ReadDouble(ref position) + "}");
                        break;

                        case "byte":
                        structure += ("{b:" + packet.ReadByte(ref position) + "}");
                        break;

                        case "boolean":
                        structure += ("{b:" + packet.ReadBoolean(ref position) + "}");
                        break;
                    }
                }
                if (packet.GetReadableBytes(position) == 0)
                {
                    entries.Add(Tuple.Create(structure + "\r\n", StructureHighlight));
                }
            }
            entries.Add(Tuple.Create("--------------------\r\n", DetailHighlight));
        }
        private void DisplayEntries(List<Tuple<string, Color>> entries)
        {
            if (!IsReceiving) return;
            foreach (Tuple<string, Color> entry in entries)
            {
                _packetLogger.LoggerTxt.SelectionStart = _packetLogger.LoggerTxt.TextLength;
                _packetLogger.LoggerTxt.SelectionLength = 0;

                _packetLogger.LoggerTxt.SelectionColor = entry.Item2;
                if (_packetLogger.LoggerTxt.Focused)
                {
                    PacketLoggerHost.Focus();
                }
                _packetLogger.LoggerTxt.AppendText(entry.Item1);
            }
        }

        private MessageItem GetMessage(DataInterceptedEventArgs args)
        {
            IDictionary<ushort, MessageItem> messages = (args.IsOutgoing ?
                App.Master.Game.OutMessages : App.Master.Game.InMessages);

            messages.TryGetValue(args.Packet.Id, out MessageItem message);
            return message;
        }
        private bool IsLoggingAuthorized(DataInterceptedEventArgs args)
        {
            if (!IsReceiving) return false;
            if (!IsDisplayingBlocked && args.IsBlocked) return false;
            if (!IsDisplayingReplaced && !args.IsOriginal) return false;

            if (!IsViewingOutgoing && args.IsOutgoing) return false;
            if (!IsViewingIncoming && !args.IsOutgoing) return false;

            if (_ignoredMessages.Count > 0)
            {
                int id = args.Packet.Id;
                if (!args.IsOutgoing)
                {
                    id = (id + ushort.MaxValue);
                }
                if (_ignoredMessages.ContainsKey(id)) return false;
            }
            return true;
        }
    }
}