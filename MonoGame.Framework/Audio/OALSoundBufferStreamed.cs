using System;
using System.Collections.Generic;
using OpenTK.Audio.OpenAL;
using NVorbis;
#if DEBUG
using System.Diagnostics;
#endif

namespace Microsoft.Xna.Framework.Audio
{
    class OALSoundBufferStreamed : OALSoundBuffer
    {
        private VorbisReader Vorbis_Reader;
        private IEnumerator<byte[]> Vorbis_Enumerator;
        private List<byte[]> Stream_Data_Arrays, Stream_Loop_Data_Arrays, Streamed_Loop_Data_Arrays;
        private List<int> streamBuffers;
        private int Channels;
        private bool hasIntroLoop = false;
        private int Blank_Length, Intro_Length, Loop_Length, Streamed_Length;
        private bool _streamedLoop = false;

        internal bool ActivelyStreaming { get { return Vorbis_Reader != null; } }
        internal bool StreamedLoop
        {
            get { return _streamedLoop; }
            set { _streamedLoop = value; }
        }


        public void BindDataBuffer(VorbisReader vorbis, ALFormat format,
            int size, int sampleRate, int introStart, int loopStart, int loopEnd)
        {
            Vorbis_Reader = vorbis;
            Vorbis_Enumerator = vorbis.GetEnumerator();
            if (vorbis.Channels > 2)
                throw new ArgumentOutOfRangeException(string.Format(
                    "Only mono and stereo sounds are supported; \"{0}\" has {1} sound channels",
                    vorbis.Name, vorbis.Channels));
            openALFormat = format;
            this.sampleRate = sampleRate;

            int bits = 16;
            Channels = vorbis.Channels;
            Blank_Length = (introStart) * ((bits / 8) * Channels);
            // If the loop start has a value, the audio needs handled so that the into and looping sections are separate
            hasIntroLoop = loopStart != -1;
            if (hasIntroLoop)
            {
                _streamedLoop = true;
                Intro_Length = (loopStart - introStart) * ((bits / 8) * Channels);
                Loop_Length = (loopEnd - loopStart) * ((bits / 8) * Channels);
            }
            else
            {
                //Intro_Length = ((int)(decoder.Samples * (bits / 8) * Channels)) - Blank_Length; //Debug
                //Loop_Length = 0;

                Intro_Length = 0;
                Loop_Length = ((int)(vorbis.TotalSamples * (bits / 8) * Channels)) - Blank_Length;
            }

            streamBuffers = new List<int>();
            Stream_Data_Arrays = new List<byte[]>();
            Stream_Loop_Data_Arrays = new List<byte[]>();
            Streamed_Loop_Data_Arrays = new List<byte[]>();

            ReadStream();
        }

        internal void ReadStream()
        {
            int length = 0;
            bool end_of_stream = false;
            List<byte[]> data = new List<byte[]>();
            // Reads data from the stream
            // If no data has been read yet, read in a minimum starting length (16 seconds?)
            // Otherwise read in 6 frames worth of music each loop (1 frame on android)

            while (!end_of_stream &&
                ((Streamed_Length < Blank_Length && length < Blank_Length) ||
#if ANDROID
                // Android streams at least 1/59th of a second of music each frame, to keep the audio seemless without bogging the CPU
                initial_stream_length_not_read(length) || ((44100 * 2 * Channels) / 59) > length))
#else
                // PC streams at least 1/10th of a second of music each frame
                initial_stream_length_not_read(length) || ((44100 * 2 * Channels) / 10) > length))
#endif
            {
                end_of_stream = !Vorbis_Enumerator.MoveNext();

                data.Add(new byte[Vorbis_Enumerator.Current.Length]);
                Array.Copy(Vorbis_Enumerator.Current, data[data.Count - 1], Vorbis_Enumerator.Current.Length);
                length += Vorbis_Enumerator.Current.Length;
            }


            byte[] intro_data_buffer = null, loop_data_buffer = null;

            int index = 0;
            int copy_index = 0, copy_length = 0;
            // If intro isn't done yet
            if (Streamed_Length < (Blank_Length + Intro_Length))
            {
                // If still needing to skip over initial blank section
                if (Streamed_Length < Blank_Length)
                {
                    // Create buffer array and copy data to it
                    intro_data_buffer = new byte[Math.Min(length - Blank_Length, Intro_Length - (Streamed_Length - Blank_Length))];
                    while (index < length && Streamed_Length < Blank_Length + Intro_Length)
                    {
                        // If we're still in the blank section before the intro, jump ahead
                        if (Streamed_Length + data[0].Length < Blank_Length)
                        {
                            length -= data[0].Length;
                            Streamed_Length += data[0].Length;
                            data.RemoveAt(0);
                        }
                        else
                        {
                            if (Streamed_Length < Blank_Length)
                                copy_index = Blank_Length - Streamed_Length;
                            copy_length = Math.Min(intro_data_buffer.Length - index, data[0].Length - copy_index);
                            Array.Copy(data[0], copy_index, intro_data_buffer, index, copy_length);
                            if (Streamed_Length < Blank_Length)
                            {
                                length -= Blank_Length - Streamed_Length;
                                Streamed_Length = Blank_Length + copy_length;
                            }
                            else
                                Streamed_Length += copy_length;
                            if (copy_index + copy_length >= data[0].Length)
                            {
                                data.RemoveAt(0);
                                copy_index = 0;
                            }
                            else
                                copy_index += copy_length;
                            index += copy_length;
                        }
                    }
                }
                // Else just copying to the intro normally
                else
                {
                    // Create buffer array and copy data to it
                    intro_data_buffer = new byte[Math.Min(length, Intro_Length - (Streamed_Length - Blank_Length))];
                    while (index < length && Streamed_Length < Blank_Length + Intro_Length)
                    {
                        copy_length = Math.Min(intro_data_buffer.Length - index, data[0].Length - copy_index);
                        Array.Copy(data[0], copy_index, intro_data_buffer, index, copy_length);
                        Streamed_Length += copy_length;
                        if (copy_index + copy_length >= data[0].Length)
                        {
                            data.RemoveAt(0);
                            copy_index = 0;
                        }
                        else
                            copy_index += copy_length;
                        index += copy_length;
                    }
                }
            }
            // Copy whatever data is remaining into the loop buffer
            if (index < length && Streamed_Length < (Blank_Length + Intro_Length + Loop_Length))
            {
                length -= index;
                index = 0;
                // Create buffer array and copy data to it
                loop_data_buffer = new byte[Math.Min(length - index, Loop_Length - (Streamed_Length - (Blank_Length + Intro_Length)))];
                while (index < length && Streamed_Length < (Blank_Length + Intro_Length + Loop_Length))
                {
                    copy_length = Math.Min(loop_data_buffer.Length - index, data[0].Length - copy_index);
                    Array.Copy(data[0], copy_index, loop_data_buffer, index, copy_length);
                    Streamed_Length += copy_length;
                    data.RemoveAt(0);
                    copy_index = 0;
                    index += copy_length;
                }
            }

            if (intro_data_buffer != null)
                Stream_Data_Arrays.Add(intro_data_buffer);
            if (loop_data_buffer != null)
                Stream_Loop_Data_Arrays.Add(loop_data_buffer);

            // If the stream is finished, clean it up
            if (end_of_stream || Streamed_Length >= (Blank_Length + Intro_Length + Loop_Length))
            {
                if (Vorbis_Reader != null)
                {
                    Vorbis_Reader.Dispose();
                    Vorbis_Reader = null;
                    Vorbis_Enumerator = null;
                }
            }
        }

        private bool initial_stream_length_not_read(int length)
        {
#if ANDROID
            // Reads 1 seconds of music up front, to prime the stream
            return Streamed_Length == 0 && (length - Blank_Length) < (44100 * 2 * Channels) * 1;
#else
            // Reads half a second of music up front, to prime the stream
            return Streamed_Length == 0 && (length - Blank_Length) < (44100 * 2 * Channels) / 2;
#endif
        }

        List<int> chunk_counts = new List<int>();
        List<int> lengths = new List<int>();

        internal void QueueStreamBuffers(int sourceId)
        {
            // Get the length of the buffer to add
            int length = 0;
            foreach (byte[] ary in Stream_Data_Arrays)
                length += ary.Length;
            foreach (byte[] ary in Stream_Loop_Data_Arrays)
                length += ary.Length;
            byte[] data = new byte[length];
#if DEBUG
            Debug.Assert(length > 0);
#endif
            // Go through intro data
            int offset = 0;
            foreach (byte[] ary in Stream_Data_Arrays)
            {
                Array.Copy(ary, 0, data, offset, ary.Length);
                offset += ary.Length;
                // If there is no intro loop, all of the data is in this list so copy from here to the list of already streamed data
                if (!hasIntroLoop)
                    Streamed_Loop_Data_Arrays.Add(ary);
            }
            chunk_counts.Add(Stream_Data_Arrays.Count);
            lengths.Add(length);
            Stream_Data_Arrays.Clear();

            // Go through loop data
            foreach (byte[] ary in Stream_Loop_Data_Arrays)
            {
                Array.Copy(ary, 0, data, offset, ary.Length);
                offset += ary.Length;
                // We only get here if there is an intro loop, so this is what should be copied to already streamed data
                Streamed_Loop_Data_Arrays.Add(ary);
            }
            Stream_Loop_Data_Arrays.Clear();

            // Create a buffer for the data
            int stream_buffer;

            ALError alError;

            alError = AL.GetError();
            AL.GenBuffers(1, out stream_buffer);
            alError = AL.GetError();
            if (alError != ALError.NoError)
            {
                Console.WriteLine("Failed to generate OpenAL data buffer: ", AL.GetErrorString(alError));
            }

            AL.BufferData(stream_buffer, openALFormat, data, data.Length, this.sampleRate);
            streamBuffers.Add(stream_buffer);
            AL.SourceQueueBuffer(sourceId, stream_buffer);
            buffers_added++;
            // If we've finished streaming, create the loop stream
            if (!ActivelyStreaming && StreamedLoop)
            {
                queue_loop_buffer(sourceId);
            }
        }

        public int buffers_added = 0; //Debug

        private void queue_loop_buffer(int sourceId)
        {
            // Get the length of the buffer to add
            int length = 0;
            foreach (byte[] ary in Streamed_Loop_Data_Arrays)
                length += ary.Length;
            byte[] data = new byte[length];
#if DEBUG
            Debug.Assert(length > 0);
#endif
            // Go through data
            int offset = 0;
            foreach (byte[] ary in Streamed_Loop_Data_Arrays)
            {
                Array.Copy(ary, 0, data, offset, ary.Length);
                offset += ary.Length;
            }
            Streamed_Loop_Data_Arrays.Clear();

            AL.BufferData(openALDataBuffer, openALFormat, data, data.Length, this.sampleRate);
            AL.SourceQueueBuffer(sourceId, openALDataBuffer);
        }

        internal void DisposePlayedBuffers(int sourceId, int count)
        {
            if (count > streamBuffers.Count)
                count = streamBuffers.Count;
            for (int i = 0; i < count; i++)
            {
                AL.SourceUnqueueBuffer(sourceId);
                streamBuffers.RemoveAt(0);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // Clean up managed objects
                }
                // Release unmanaged resources
                if (Vorbis_Reader != null)
                {
                    Vorbis_Reader.Dispose();
                    Vorbis_Reader = null;
                    Vorbis_Enumerator = null;
                }

                int buffer;
                if (streamBuffers != null)
                {
                    foreach (int stream_buffer in streamBuffers)
                    {
                        buffer = stream_buffer;
                        AL.DeleteBuffers(1, ref buffer);
                    }
                    streamBuffers.Clear();
                    streamBuffers = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}