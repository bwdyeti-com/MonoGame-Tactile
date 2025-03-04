// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.IO;

#if XNA
namespace MonoGame.Framework.Audio
#else
namespace Microsoft.Xna.Framework.Audio
#endif
{
    class VolumeEvent : ClipEvent
	{
	    private readonly float _volume;

        public VolumeEvent(XactClip clip, float timeStamp, float randomOffset, float volume)
            : base(clip, timeStamp, randomOffset)
        {
            _volume = volume;
        }

		public override void Play() 
        {
            _clip.SetVolume(_volume);
        }

	    public override void Stop()
	    {
	    }

	    public override void Pause() 
        {
		}

		public override void Resume()
		{
		}

		public override void SetTrackVolume(float volume)
        {
		}

	    public override bool Update(float dt)
	    {
	        return false;
	    }

	    public override void SetFade(float fadeInDuration, float fadeOutDuration)
        {
        }

        public override void Apply3D(AudioListener listener, AudioEmitter emitter)
        {
        }
    }
}

