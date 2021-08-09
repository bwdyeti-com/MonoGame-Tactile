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
    /// <summary>Controls how Cue objects should cease playback when told to stop.</summary>
	public enum AudioStopOptions
	{
        /// <summary>Stop normally, playing any pending release phases or transitions.</summary>
		AsAuthored,
        /// <summary>Immediately stops the cue, ignoring any pending release phases or transitions.</summary>
		Immediate
	}
}

