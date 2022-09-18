namespace Tweey.Renderer.VertexArrayObjects;

abstract class BaseVertexArrayObject
{
    protected static BaseVertexArrayObject? lastBoundVertexArray;

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
