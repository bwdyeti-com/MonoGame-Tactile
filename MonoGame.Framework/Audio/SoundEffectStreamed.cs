using System;
using System.IO;

#if MONOMAC && PLATFORM_MACOS_LEGACY
using MonoMac.AudioToolbox;
using MonoMac.AudioUnit;
using MonoMac.AVFoundation;
using MonoMac.Foundation;
using MonoMac.OpenAL;
#elif OPENAL
using OpenTK.Audio.OpenAL;
#if IOS || MONOMAC
using AudioToolbox;
using AudioUnit;
using AVFoundation;
using Foundation;
#endif
#endif

#if XNA
namespace MonoGame.Framework.Audio
#else
namespace Microsoft.Xna.Framework.Audio
#endif
{
    public sealed class SoundEffectStreamed : SoundEffect
    {
        [CLSCompliant(false)]
        public static SoundEffectStreamed FromVorbis(NVorbis.VorbisReader vorbis,
            int introStart, int loopStart, int loopEnd)
        {
            if (vorbis == null)
                throw new ArgumentNullException();

            // Notes from the docs:

            /*The Stream object must point to the head of a valid PCM wave file. Also, this wave file must be in the RIFF bitstream format.
              The audio format has the following restrictions:
              Must be a PCM wave file
              Can only be mono or stereo
              Must be 8 or 16 bit
              Sample rate must be between 8,000 Hz and 48,000 Hz*/

            var sfx = new SoundEffectStreamed();

            sfx._introStart = introStart;
            sfx._loopStart = loopStart;
            sfx._loopEnd = loopEnd;

            sfx.PlatformInitialize(vorbis, introStart, loopStart, loopEnd);

            return sfx;
        }

        private void PlatformInitialize(NVorbis.VorbisReader vorbis,
            int introStart, int loopStart, int loopEnd)
        {
            Format = vorbis.Channels == 2 ? ALFormat.Stereo16 : ALFormat.Mono16;
            Rate = vorbis.SampleRate;

            var buffer = new OALSoundBufferStreamed();
            buffer.BindDataBuffer(vorbis, Format, Size, (int)Rate,
                introStart, loopStart, loopEnd);
            SoundBuffer = buffer;
        }
    }
}
