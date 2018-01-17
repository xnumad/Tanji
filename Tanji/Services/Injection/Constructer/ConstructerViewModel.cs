using System;
using System.IO;
using System.Windows;
using System.ComponentModel;
using System.Collections.Specialized;

using Microsoft.Win32;

using Tanji.Helpers;
using Tanji.Services.Injection.Constructer.Models;

namespace Tanji.Services.Injection.Constructer
{
    public class ConstructerViewModel : ObservableObject
    {
        private readonly SaveFileDialog _saveChunksDialog;
        private readonly OpenFileDialog _loadChunksDialog;

        private const byte MAX_CHUNKS = byte.MaxValue;

        private ushort _id;
        public ushort Id
        {
            get => _id;
            set
            {
                _id = value;
                RaiseOnPropertyChanged();
                RaiseOnPropertyChanged(nameof(Signature));
            }
        }

        private byte _quantity = 1;
        public byte Quantity
        {
            get => _quantity;
            set
            {
                if (value == 0)
                {
                    value = 1;
                }

                _quantity = value;
                RaiseOnPropertyChanged();
            }
        }

        private string _value = string.Empty;
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                RaiseOnPropertyChanged();
            }
        }

        public string Signature => ("{id:" + Id + "}" + string.Join(string.Empty, Chunks));

        public Command CopyCommand { get; }
        public Command SaveCommand { get; }
        public Command LoadCommand { get; }
        public Command ClearCommand { get; }
        public Command<Type> WriteCommand { get; }
        public ObservableRangeCollection<Chunk> Chunks { get; }

        public ConstructerViewModel()
        {
            _saveChunksDialog = new SaveFileDialog
            {
                DefaultExt = "chks",
                Title = "Tanji - Save Chunks",
                Filter = "Chunks (*.chks)|*.chks"
            };
            _loadChunksDialog = new OpenFileDialog
            {
                Title = "Tanji - Load Chunks",
                Filter = "Chunks (*.chks)|*.chks"
            };

            CopyCommand = new Command(Copy);
            SaveCommand = new Command(Save, CanSave);
            LoadCommand = new Command(Load);

            ClearCommand = new Command(Clear, CanClear);
            WriteCommand = new Command<Type>(Write, CanWrite);

            Chunks = new ObservableRangeCollection<Chunk>();
            Chunks.CollectionChanged += Chunks_CollectionChanged;
        }

        private void Chunk_PropertyChanged(object sender, PropertyChangedEventArgs e)
      {
            if (e.PropertyName != nameof(Chunk.Value)) return;
            RaiseOnPropertyChanged(nameof(Chunks));
            RaiseOnPropertyChanged(nameof(Signature));
        }
        private void Chunks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Chunk chunk in e.NewItems)
                {
                    chunk.PropertyChanged += Chunk_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (Chunk chunk in e.OldItems)
                {
                    chunk.PropertyChanged -= Chunk_PropertyChanged;
                }
            }
            RaiseOnPropertyChanged(nameof(Chunks));
            RaiseOnPropertyChanged(nameof(Signature));
        }

        public void Write(Type targetType)
        {
            for (int i = 0; i < Quantity; i++)
            {
                Chunks.Add(new Chunk(this, Value, Type.GetTypeCode(targetType)));
            }
        }
        private bool CanWrite(Type targetType)
        {
            return (Quantity + Chunks.Count) <= MAX_CHUNKS;
        }

        private void Clear(object obj)
        {
            Chunks.Clear();
        }
        private bool CanClear(object obj)
        {
            return (Chunks.Count > 0);
        }

        private void Copy(object obj)
        {
            Clipboard.SetText(Signature);
        }
        private void Load(object obj)
        {
            _loadChunksDialog.FileName = string.Empty;
            if (_loadChunksDialog.ShowDialog() ?? false)
            {
                using (var chksStream = File.OpenRead(_loadChunksDialog.FileName))
                using (var chksReader = new BinaryReader(chksStream))
                {
                    Id = chksReader.ReadUInt16();
                    int chunkCount = chksReader.ReadInt32();

                    Chunks.Clear();
                    for (int i = 0; i < chunkCount; i++)
                    {
                        string value = chksReader.ReadString();
                        var code = (TypeCode)chksReader.ReadByte();
                        Chunks.Add(new Chunk(this, value, code));
                    }
                }
            }
        }

        private void Save(object obj)
        {
            _saveChunksDialog.FileName = string.Empty;
            if (_saveChunksDialog.ShowDialog() ?? false)
            {
                using (var chksStream = File.Open(_saveChunksDialog.FileName, FileMode.Create))
                using (var chksWriter = new BinaryWriter(chksStream))
                {
                    chksWriter.Write(Id);
                    chksWriter.Write(Chunks.Count);
                    foreach (Chunk chunk in Chunks)
                    {
                        chksWriter.Write(chunk.Value);
                        chksWriter.Write((byte)chunk.Code);
                    }
                }
            }
        }
        private bool CanSave(object obj)
        {
            return (Chunks.Count > 0);
        }
    }
}