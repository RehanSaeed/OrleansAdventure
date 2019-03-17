using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Orleans.Runtime;

namespace AdventureGrains
{
    public class MyComponent : ILifecycleParticipant<IGrainLifecycle>
    {
        private IGrainActivationContext context;

        public MyComponent(IGrainActivationContext context)
        {
            this.context = context;
        }

        public static MyComponent Create(IGrainActivationContext context)
        {
            var component = new MyComponent(context);
            component.Participate(context.ObservableLifecycle);
            return component;
        }

        public void Participate(IGrainLifecycle lifecycle)
        {
            lifecycle.Subscribe<MyComponent>(GrainLifecycleStage.Activate, OnActivate);
            lifecycle.Subscribe<MyComponent>(GrainLifecycleStage.SetupState, OnSetupState);
        }

        private Task OnActivate(CancellationToken ct)
        {
            // Do stuff
            return Task.CompletedTask;
        }

        private Task OnSetupState(CancellationToken ct)
        {
            // Do stuff
            return Task.CompletedTask;
        }
    }
}
