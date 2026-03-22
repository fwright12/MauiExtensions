using Microsoft.Maui.Controls.Compatibility;

namespace Microsoft.Maui.Controls
{
    public abstract class AddStatefulStyleBehavior : Behavior<VisualElement>
    {
        protected override void OnAttachedTo(VisualElement bindable)
        {
            base.OnAttachedTo(bindable);

            Add(bindable);
            bindable.Behaviors.Remove(this);
        }

        protected abstract void Add(VisualElement bindable);
    }

    [ContentProperty(nameof(ActionTypes))]
    public class AddStatefulTriggerActionBehavior : AddStatefulStyleBehavior
    {
        public string? Event { get; set; }
        public List<Type>? ActionTypes { get; } = new List<Type>();

        protected override void Add(VisualElement bindable)
        {
            var trigger = new EventTrigger
            {
                Event = Event,
            };
            foreach (var triggerAction in CreateTriggerActions())
            {
                trigger.Actions.Add(triggerAction);
            }

            bindable.Triggers.Add(trigger);
        }

        protected virtual IEnumerable<TriggerAction> CreateTriggerActions()
        {
            if (ActionTypes == null)
            {
                yield break;
            }

            foreach (var actionType in ActionTypes)
            {
                if (Activator.CreateInstance(actionType) is TriggerAction triggerAction)
                {
                    yield return triggerAction;
                }
            }
        }
    }

    public class AddStatefulTriggerActionBehavior<T> : AddStatefulTriggerActionBehavior
        where T : TriggerAction, new()
    {
        protected override IEnumerable<TriggerAction> CreateTriggerActions() =>
        [
            new T()
        ];
    }

    public class AddStatefulBehaviorBehavior : AddStatefulStyleBehavior
    {
        public Type? BehaviorType { get; }

        protected override void Add(VisualElement bindable)
        {
            if (CreateBehavior() is Behavior behavior)
            {
                bindable.Behaviors.Add(behavior);
            }
        }

        protected virtual Behavior? CreateBehavior()
        {
            if (BehaviorType == null)
            {
                return null;
            }

            return Activator.CreateInstance(BehaviorType) as Behavior;
        }
    }

    public class AddStatefulBehaviorBehavior<T> : AddStatefulBehaviorBehavior
        where T : Behavior, new()
    {
        protected override Behavior? CreateBehavior() => new T();
    }
}

namespace Microsoft.Maui.Controls.Extensions
{
    public class HideTabBarOnScrollTrigger : TriggerAction<Controls.ScrollView>
    {
        private double LastScrollX;
        private double LastScrollY;

        protected override void Invoke(Controls.ScrollView sender)
        {
            Shell.SetTabBarIsVisible(sender.Parent<Page>(), sender.ScrollX < LastScrollX || sender.ScrollY < LastScrollY);
            
            LastScrollX = sender.ScrollX;
            LastScrollY = sender.ScrollY;
        }
    }
}
