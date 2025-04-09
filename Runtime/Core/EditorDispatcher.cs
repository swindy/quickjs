using System;
using System.Collections;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

namespace QuickJS.Core
{
public class EditorDispatcher : BaseDispatcher<object>
    {
#if UNITY_EDITOR
        static EventInfo TickEvent;
        static MethodInfo AddTickMethod;
        static MethodInfo RemoveTickMethod;

        Delegate TickDelegate;

        static EditorDispatcher()
        {
            TickEvent = typeof(EditorApplication).GetEvent("tick", BindingFlags.NonPublic | BindingFlags.Static);
            AddTickMethod = TickEvent?.GetAddMethod(true);
            RemoveTickMethod = TickEvent?.GetRemoveMethod(true);
        }

        void AddTick(Delegate tick)
        {
            AddTickMethod?.Invoke(null, new object[] { tick });
        }

        void RemoveTick(Delegate tick)
        {
            RemoveTickMethod?.Invoke(null, new object[] { tick });
        }

        public override void Dispose()
        {
            base.Dispose();

            RemoveTick(TickDelegate);
            EditorApplication.update -= LateUpdate;
            EditorApplication.playModeStateChanged -= PlayStateChange;
        }

        private void PlayStateChange(PlayModeStateChange obj)
        {
            RemoveTick(TickDelegate);
            AddTick(TickDelegate);
            EditorApplication.update -= LateUpdate;
            EditorApplication.update += LateUpdate;
        }
#endif


        public EditorDispatcher(EngineContext ctx)
        {
            Scheduler = new DefaultScheduler(this, ctx);
#if UNITY_EDITOR
            TickDelegate = TickEvent == null ? null : Delegate.CreateDelegate(TickEvent.EventHandlerType, this, this.GetType().GetMethod(nameof(Update)));

            AddTick(TickDelegate);
            EditorApplication.update += LateUpdate;
            EditorApplication.playModeStateChanged += PlayStateChange;
#endif
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override object StartCoroutine(IEnumerator cr) => null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void StopCoroutine(object cr) { }


        protected override IEnumerator TimeoutCoroutine(Action callback, float time, int handle)
        {
            yield return null;
            if (!ToStop.Contains(handle)) callback();
        }

        protected override IEnumerator IntervalCoroutine(Action callback, float interval, int handle)
        {

            while (true)
            {
                yield return null;
                if (!ToStop.Contains(handle)) callback();
                else break;
            }
        }

        protected override IEnumerator AnimationFrameCoroutine(Action callback, int handle)
        {
            yield return null;
            if (!ToStop.Contains(handle)) callback();
        }
    }
}