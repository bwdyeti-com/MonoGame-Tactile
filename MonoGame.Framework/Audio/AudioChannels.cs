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
    /// <summary>
    /// Represents how many channels are used in the audio data.
    /// </summary>
    public enum AudioChannels
    {
        /// <summary>Single channel.</summary> 
        Mono = 1,
        /// <summary>Two channels.</summary> 
        Stereo = 2
    }
}

