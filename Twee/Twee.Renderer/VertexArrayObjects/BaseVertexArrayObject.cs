using System.Reflection;

namespace Twee.Renderer.VertexArrayObjects;

public abstract class BaseVertexArrayObject
{
    protected static Action<Type, VertexArrayHandle>? VertexDefinitionSetup { get; }
    protected static BaseVertexArrayObject? LastBoundVertexArray { get; set; }

    static BaseVertexArrayObject()
    {
        // fill in the VertexDefinitionSetup function
        if(Assembly.GetEntryAssembly()?.GetType("Twee.Renderer.Support.VertexDefinitionSetup") is { } vdsType
            && vdsType.GetMethod("Setup", BindingFlags.Static | BindingFlags.Public) is { } setupMethod)
        {
            VertexDefinitionSetup = (vertexType, vah) =>
                setupMethod.Invoke(null, new object[] { vertexType, vah });
        }
    }

    protected static void AddFrameData(PrimitiveType primitiveType, ulong count)
    {
        if (primitiveType is PrimitiveType.Lines)
            FrameData.NewLineDraw(count / 2);
        else if (primitiveType is PrimitiveType.Triangles)
            FrameData.NewTriangleDraw(count / 3);
        else
            throw new NotImplementedException();
    }

    public abstract void Draw(PrimitiveType primitiveType, int vertexOrIndexOffset = 0, int vertexOrIndexCount = -1);
}
