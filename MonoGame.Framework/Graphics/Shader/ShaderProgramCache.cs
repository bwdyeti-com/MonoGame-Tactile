#if OPENGL

using System;
using System.Collections.Generic;

#if MONOMAC
#if PLATFORM_MACOS_LEGACY
using MonoMac.OpenGL;
using GetProgramParameterName = MonoMac.OpenGL.ProgramParameter;
#else
using OpenTK.Graphics.OpenGL;
using GetProgramParameterName = OpenTK.Graphics.OpenGL.ProgramParameter;
#endif
#elif DESKTOPGL
using OpenTK.Graphics.OpenGL;
#elif WINRT

#else
using OpenTK.Graphics.ES20;
#if IOS || ANDROID
//using GetProgramParameterName = OpenTK.Graphics.ES30.ProgramParameter; //@Debug
using GetProgramParameterName = OpenTK.Graphics.ES20.ProgramParameter;
#endif
#endif

namespace Microsoft.Xna.Framework.Graphics
{

    internal class ShaderProgram
    {
        public readonly int Program;

        private readonly Dictionary<string, int> _uniformLocations = new Dictionary<string, int>();

        public ShaderProgram(int program)
        {
            Program = program;
        }

        public int GetUniformLocation(string name)
        {
            if (_uniformLocations.ContainsKey(name))
                return _uniformLocations[name];

            var location = GL.GetUniformLocation(Program, name);
            GraphicsExtensions.CheckGLError();
            _uniformLocations[name] = location;
            return location;
        }
    }

    /// <summary>
    /// This class is used to Cache the links between Vertex/Pixel Shaders and Constant Buffers.
    /// It will be responsible for linking the programs under OpenGL if they have not been linked
    /// before. If an existing link exists it will be resused.
    /// </summary>
    internal class ShaderProgramCache : IDisposable
    {
        private readonly Dictionary<int, ShaderProgram> _programCache = new Dictionary<int, ShaderProgram>();
        bool disposed;

        ~ShaderProgramCache()
        {
            Dispose(false);
        }

        /// <summary>
        /// Clear the program cache releasing all shader programs.
        /// </summary>
        public void Clear()
        {
            foreach (var pair in _programCache)
            {
                if (GL.IsProgram(pair.Value.Program))
                {
#if MONOMAC
                    GL.DeleteProgram(pair.Value.Program, null);
#else
                    GL.DeleteProgram(pair.Value.Program);
#endif
                    GraphicsExtensions.CheckGLError();
                }
            }
            _programCache.Clear();
        }

        public ShaderProgram GetProgram(Shader vertexShader, Shader pixelShader)
        {
            // TODO: We should be hashing in the mix of constant 
            // buffers here as well.  This would allow us to optimize
            // setting uniforms to only when a constant buffer changes.

            var key = vertexShader.HashKey | pixelShader.HashKey;
            if (!_programCache.ContainsKey(key))
            {
                // the key does not exist so we need to link the programs
                _programCache.Add(key, Link(vertexShader, pixelShader));
            }

            return _programCache[key];
        }

        private ShaderProgram Link(Shader vertexShader, Shader pixelShader)
        {
            // NOTE: No need to worry about background threads here
            // as this is only called at draw time when we're in the
            // main drawing thread.
            var program = GL.CreateProgram();
            GraphicsExtensions.CheckGLError();

            GL.AttachShader(program, vertexShader.GetShaderHandle());
            GraphicsExtensions.CheckGLError();

            GL.AttachShader(program, pixelShader.GetShaderHandle());
            GraphicsExtensions.CheckGLError();

            //vertexShader.BindVertexAttributes(program);

            GL.LinkProgram(program);
            GraphicsExtensions.CheckGLError();

            GL.UseProgram(program);
            GraphicsExtensions.CheckGLError();

            vertexShader.GetVertexAttributeLocations(program);

            pixelShader.ApplySamplerTextureUnits(program);

            var linked = 0;

            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out linked);
            GraphicsExtensions.LogGLError("VertexShaderCache.Link(), GL.GetProgram");
            if (linked == 0)
            {
                var log = GL.GetProgramInfoLog(program);
                Console.WriteLine(log);
                GL.DetachShader(program, vertexShader.GetShaderHandle());
                GL.DetachShader(program, pixelShader.GetShaderHandle());
#if MONOMAC
                GL.DeleteProgram(1, ref program);
#else
                GL.DeleteProgram(program);
#endif
                throw new InvalidOperationException("Unable to link effect program");
            }

            return new ShaderProgram(program);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                    Clear();
                disposed = true;
            }
        }
    }
}

#endif // OPENGL
