using Armine.Model.Module;
using UnityEngine;

namespace Armine.Utils
{
	public class Progress
	{
		#region Members
		private const float refreshRate = 0.04f; // In seconds

		private object progressMutex = new object();
		private ProgressCallback progress;
		private uint nbSteps;
		private uint currentStep;
		private float nextDisplay;
		#endregion

		#region Getter / Setter
		internal uint CurrentStep
		{
			get
			{
				return currentStep;
			}
		}

		internal uint TotalSteps
		{
			get
			{
				return nbSteps;
			}
		}
		#endregion

		#region Public methods
		internal void Clean()
		{
			progress = null;
			nbSteps = 0;
			currentStep = 0;
		}

		internal void Init(uint nb_steps, ProgressCallback callback = null)
		{
			progress = callback;
			nbSteps = nb_steps;
			currentStep = 0;

			nextDisplay = Time.realtimeSinceStartup;
		}

		internal void Set(uint step)
		{
			lock(progressMutex)
			{
				currentStep = step;
			}
		}

		internal void Update(uint factor)
		{
			lock(progressMutex)
			{
				currentStep += factor;
			}
		}

		internal void Display()
		{
			if(progress != null)
			{
				float percentage = -1f;

				lock(progressMutex)
				{
					if(Time.realtimeSinceStartup >= nextDisplay)
					{
						percentage = (float) currentStep / (float) nbSteps;

						nextDisplay = Time.realtimeSinceStartup + refreshRate;
					}
				}

				if(percentage >= 0f)
				{
					progress(percentage);
				}
			}
		}
		#endregion
	}
}