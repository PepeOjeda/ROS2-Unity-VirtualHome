using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CustomTimers
{
    public enum TimerType {RealTime, Scaled}
	public class Countdown
	{
		double start;
		double length;
        bool realTime;

        double time
        {
            get{
                if(realTime)
                    return Time.realtimeSinceStartupAsDouble;
                else
                    return Time.timeAsDouble;
            }
        }

		public Countdown(TimerType type = TimerType.Scaled)
		{
            realTime = type == TimerType.RealTime;
			start = 0;
			length = 0;
			frozen = false;
		}

		public Countdown(double _length, TimerType type = TimerType.Scaled)
		{
            realTime = type == TimerType.RealTime;
			start = time;
			length = _length;
			frozen = false;
		}

		public void Restart()
		{
			start = time;
		}

		public void Restart(double _length)
		{
			start = time;
			length = _length;
			frozen = false;
		}



		public bool frozen { get; private set; }
		double timeEllapsedAtFreeze = double.NegativeInfinity;
		public void Freeze()
		{
			frozen = true;
			timeEllapsedAtFreeze = time - start;
		}

		public void Unfreeze()
		{
			if (frozen)
				start = time - timeEllapsedAtFreeze;
			frozen = false;
		}

		double currentTime
		{
			get
			{
				if (frozen)
					return start + timeEllapsedAtFreeze;
				else
					return time;
			}

		}

		public bool done => currentTime - start >= length;

		public float proportionComplete
		{
			get
			{
				if (length <= 0)
					return 1;
				else
					return (float)((currentTime - start) / length);
			}
		}
	}

	public class Stopwatch
	{
		double start;
		public Stopwatch()
		{
			start = Time.timeAsDouble;
		}

		public void Restart()
		{
			start = Time.timeAsDouble;
		}

		public double ellapsed => Time.timeAsDouble - start;

	}

}