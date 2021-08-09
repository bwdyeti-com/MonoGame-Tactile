// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;

#if XNA
namespace MonoGame.Framework.Audio
#else
namespace Microsoft.Xna.Framework.Audio
#endif
{
    /// <summary>Described the playback state of a SoundEffectInstance.</summary>
    public enum SoundState
    {
        /// <summary>The SoundEffectInstance has not started or failed to start.</summary>
        Initial,
        /// <summary>The SoundEffectInstance is currently playing.</summary>
        Playing,
        /// <summary>The SoundEffectInstance is currently paused.</summary>
        Paused,
        /// <summary>The SoundEffectInstance is currently stopped.</summary>
        Stopped
    }
}

