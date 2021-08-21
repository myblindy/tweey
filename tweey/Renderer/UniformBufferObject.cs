﻿namespace Tweey.Renderer
{
    class UniformBufferObject<T> where T : unmanaged
    {
        public int Name { get; }

        T data;
        public ref T Data => ref data;

        public UniformBufferObject()
        {
            GL.CreateBuffers(1, out int name);
            Name = name;
            GL.NamedBufferData(Name, Unsafe.SizeOf<T>(), IntPtr.Zero, BufferUsageHint.DynamicDraw);
        }

        public void Update() =>
            GL.NamedBufferSubData(Name, IntPtr.Zero, Unsafe.SizeOf<T>(), ref data);

        public void Bind(int bindingPoint) =>
            GL.BindBufferBase(BufferRangeTarget.UniformBuffer, bindingPoint, Name);
    }
}
