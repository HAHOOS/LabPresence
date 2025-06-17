using System;
using System.Collections.Generic;
using System.Linq;

using DiscordRPC.Message;

namespace LabPresence.Plugins
{
    public static class Overwrites
    {
        public static OverwritesType OnLevelLoaded { get; } = new();

        public static OverwritesType OnLevelLoading { get; } = new();

        public static OverwritesType OnLevelUnloaded { get; } = new();

        public static OverwritesType OnAssetWarehouseLoaded { get; } = new();

        public static OverwritesType OnPreGame { get; } = new();

        public static OverwritesType<JoinMessage> OnJoin { get; } = new();

        public static OverwritesType<JoinRequestMessage> OnJoinRequested { get; } = new();
        public static OverwritesType<SpectateMessage> OnSpectate { get; } = new();
    }

    public class OverwritesType<T>() where T : new()
    {
        private readonly List<Overwrite<T>> _Overwrites = [];

        public IReadOnlyCollection<Overwrite<T>> Overwrites => _Overwrites.AsReadOnly();

        internal Action<T> Default { get; private set; }

        internal void SetDefault(Action<T> action)
            => Default = action;

        public void RegisterOverwrite(Overwrite<T> overwrite)
        {
            if (IsRegistered(overwrite))
                throw new Exception("An overwrite with the same ID is already registered!");

            _Overwrites.Add(overwrite);
        }

        public void RegisterOverwrite(Func<T, bool> callback, out Guid ID, int priority = 0)
        {
            var overwrite = new Overwrite<T>(callback, priority);
            RegisterOverwrite(overwrite);
            ID = overwrite.ID;
        }

        public bool RemoveOverwrite(Guid ID)
            => _Overwrites.RemoveAll(x => x.ID == ID) > 0;

        public bool RemoveOverwrite(Overwrite<T> overwrite)
            => RemoveOverwrite(overwrite.ID);

        public bool IsRegistered(Guid ID) => Overwrites.Any(x => x.ID == ID);

        public bool IsRegistered(Overwrite<T> overwrite) => IsRegistered(overwrite.ID);

        public void Run(T arg)
        {
            if (_Overwrites.Count == 0)
            {
                Default?.Invoke(arg);
                return;
            }

            var ordered = Overwrites.OrderByDescending(x => x.Priority).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered.Count == 0)
                    break;

                var overwrite = ordered[0];
                if (overwrite == null)
                {
                    ordered.RemoveAt(0);
                    continue;
                }

                bool result = overwrite.Callback?.Invoke(arg) == true;
                if (!result)
                {
                    ordered.RemoveAt(0);
                    continue;
                }
                else
                {
                    return;
                }
            }
            Default?.Invoke(arg);
        }
    }

    public class Overwrite<T> where T : new()
    {
        public int Priority { get; set; }

        public Guid ID { get; set; }

        public Func<T, bool> Callback { get; set; }

        public Overwrite(Guid id, Func<T, bool> callback, int priority = 0)
        {
            ID = id;
            Priority = priority;
            Callback = callback;
        }

        public Overwrite(Func<T, bool> callback, int priority = 0)
        {
            ID = Guid.NewGuid();
            Priority = priority;
            Callback = callback;
        }
    }

    public class OverwritesType()
    {
        private readonly List<Overwrite> _Overwrites = [];

        public IReadOnlyCollection<Overwrite> Overwrites => _Overwrites.AsReadOnly();

        internal Action Default { get; private set; }

        internal void SetDefault(Action action)
            => Default = action;

        public void RegisterOverwrite(Overwrite overwrite)
        {
            if (IsRegistered(overwrite))
                throw new Exception("An overwrite with the same ID is already registered!");

            _Overwrites.Add(overwrite);
        }

        public void RegisterOverwrite(Func<bool> callback, out Guid ID, int priority = 0)
        {
            var overwrite = new Overwrite(callback, priority);
            RegisterOverwrite(overwrite);
            ID = overwrite.ID;
        }

        public bool RemoveOverwrite(Guid ID)
            => _Overwrites.RemoveAll(x => x.ID == ID) > 0;

        public bool RemoveOverwrite(Overwrite overwrite)
            => RemoveOverwrite(overwrite.ID);

        public bool IsRegistered(Guid ID) => Overwrites.Any(x => x.ID == ID);

        public bool IsRegistered(Overwrite overwrite) => IsRegistered(overwrite.ID);

        public void Run()
        {
            if (_Overwrites.Count == 0)
            {
                Default?.Invoke();
                return;
            }

            var ordered = Overwrites.OrderByDescending(x => x.Priority).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered.Count == 0)
                    break;

                var overwrite = ordered[0];
                if (overwrite == null)
                {
                    ordered.RemoveAt(0);
                    continue;
                }

                bool result = overwrite.Callback?.Invoke() == true;
                if (!result)
                {
                    ordered.RemoveAt(0);
                    continue;
                }
                else
                {
                    return;
                }
            }
            Default?.Invoke();
        }
    }

    public class Overwrite
    {
        public int Priority { get; set; }

        public Guid ID { get; set; }

        public Func<bool> Callback { get; set; }

        public Overwrite(Guid id, Func<bool> callback, int priority = 0)
        {
            ID = id;
            Priority = priority;
            Callback = callback;
        }

        public Overwrite(Func<bool> callback, int priority = 0)
        {
            ID = Guid.NewGuid();
            Priority = priority;
            Callback = callback;
        }
    }
}