/*
 * TimerHandle.cs
 * --------------
 * Module:  Core / Timer
 * Purpose: Handle object returned by TimerService.SetTimeout/SetInterval. Provides a
 *          Cancel() method to abort a scheduled timer before it fires, and an IsCancelled
 *          read-only property to check cancellation state. Holds a reference to the
 *          originating TimerService (not used for cancellation — cancellation is purely
 *          local via the IsCancelled flag checked by the coroutine loop).
 * Dependencies: TimerService (constructor parameter, stored but not actively used).
 * Scene:    N/A (value object; created and discarded per timer request).
 * Ch.Ref:   Ch.24.2 Timer Service.
 */
using System;

namespace Zephyr.Core.Timer
{
    /// <summary>
    /// Handle to a scheduled timer. Call Cancel() to abort before it fires.
    /// </summary>
    public class TimerHandle
    {
        private TimerService _service;

        public bool IsCancelled { get; private set; }

        public TimerHandle(TimerService service)
        {
            _service = service;
        }

        public void Cancel()
        {
            IsCancelled = true;
        }
    }
}
