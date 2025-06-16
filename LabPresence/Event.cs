using System;
using System.Collections.Generic;
using System.Linq;

using LabPresence.Helper;

namespace LabPresence
{
    public class Event
    {
        private readonly List<EventAction> _actions = [];

        public IReadOnlyCollection<EventAction> Actions => _actions.AsReadOnly();

        public void Subscribe(EventAction action)
        {
            if (_actions.Contains(action) || _actions.Any(x => x.ID == action.ID))
                throw new Exception("An EventAction with the same ID is already subscribed!");

            _actions.Add(action);
        }

        public void Subscribe(Action action, out Guid ID, int priority = 0)
        {
            var eventAction = new EventAction(action, priority);
            ID = eventAction.ID;
            Subscribe(eventAction);
        }

        public bool Unsubscribe(Action action)
            => _actions.RemoveFirst(x => x.Action == action);

        public bool Unsubscribe(Guid ID)
            => _actions.RemoveFirst(X => X.ID == ID);

        public bool Unsubscribe(EventAction action)
            => _actions.Remove(action);

        public void UnsubscribeAll()
            => _actions.Clear();

        public void Invoke()
        {
            if (_actions.Count == 0)
                return;

            var ordered = _actions.OrderByDescending(x => x.Priority).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered.Count == 0)
                    break;

                var action = ordered[0];
                if (action == null)
                {
                    ordered.RemoveAt(0);
                    continue;
                }

                try
                {
                    action?.Action?.Invoke();
                }
                catch (Exception ex)
                {
                    Core.Logger.Error(ex);
                }
            }
        }
    }

    public class EventAction
    {
        public EventAction(Action action, int priority = 0)
        {
            Action = action;
            ID = Guid.NewGuid();
            Priority = priority;
        }

        public Action Action { get; set; }

        public Guid ID { get; set; }

        public int Priority { get; set; }
    }

    public class Event<T> where T : EventArgs
    {
        private readonly List<EventAction<T>> _actions = [];

        public IReadOnlyCollection<EventAction<T>> Actions => _actions.AsReadOnly();

        public void Subscribe(EventAction<T> action)
        {
            if (_actions.Contains(action) || _actions.Any(x => x.ID == action.ID))
                throw new Exception("An EventAction with the same ID is already subscribed!");

            _actions.Add(action);
        }

        public void Subscribe(Action<T> action, out Guid ID, int priority = 0)
        {
            var eventAction = new EventAction<T>(action, priority);
            ID = eventAction.ID;
            Subscribe(eventAction);
        }

        public bool Unsubscribe(Action<T> action)
            => _actions.RemoveFirst(x => x.Action == action);

        public bool Unsubscribe(Guid ID)
            => _actions.RemoveFirst(X => X.ID == ID);

        public bool Unsubscribe(EventAction<T> action)
            => _actions.Remove(action);

        public void UnsubscribeAll()
            => _actions.Clear();

        public void Invoke(T arg)
        {
            if (_actions.Count == 0)
                return;

            var ordered = _actions.OrderByDescending(x => x.Priority).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered.Count == 0)
                    break;

                var action = ordered[0];
                if (action == null)
                {
                    ordered.RemoveAt(0);
                    continue;
                }

                try
                {
                    action?.Action?.Invoke(arg);
                }
                catch (Exception ex)
                {
                    Core.Logger.Error(ex);
                }
            }
        }
    }

    public class EventAction<T> where T : EventArgs
    {
        public EventAction(Action<T> action, int priority = 0)
        {
            Action = action;
            ID = Guid.NewGuid();
            Priority = priority;
        }

        public Action<T> Action { get; set; }

        public Guid ID { get; set; }

        public int Priority { get; set; }
    }
}