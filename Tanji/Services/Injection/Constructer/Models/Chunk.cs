using System;

using Tanji.Helpers;

namespace Tanji.Services.Injection.Constructer.Models
{
    public class Chunk : ObservableObject
    {
        private readonly ConstructerViewModel _viewModel;

        public TypeCode Code { get; }
        public int Index => _viewModel.Chunks.IndexOf(this);

        private string _value = null;
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                RaiseOnPropertyChanged();
            }
        }

        public Command PushCommand { get; }
        public Command PullCommand { get; }
        public Command RemoveCommand { get; }

        public Chunk(ConstructerViewModel viewModel, string value, TypeCode code)
        {
            _viewModel = viewModel;

            Code = code;
            Value = value;

            RemoveCommand = new Command(Remove);
            PushCommand = new Command(Push, CanPush);
            PullCommand = new Command(Pull, CanPull);
        }

        private void Push(object obj)
        {
            _viewModel.Chunks.Move(Index, Index - 1);
        }
        private bool CanPush(object obj)
        {
            return (Index != 0);
        }

        private void Pull(object obj)
        {
            _viewModel.Chunks.Move(Index, Index + 1);
        }
        private bool CanPull(object obj)
        {
            return (Index != (_viewModel.Chunks.Count - 1));
        }

        private void Remove(object obj)
        {
            _viewModel.Chunks.RemoveAt(Index);
        }

        public override string ToString()
        {
            return ("{" + Code.ToString().ToLower()[0] + ":" + Value + "}");
        }
    }
}