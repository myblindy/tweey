namespace Tweey.Renderer.VertexArrayObjects;

abstract class BaseVertexArrayObject
{
    protected static BaseVertexArrayObject? lastBoundVertexArray;

    protected void AddFrameData(PrimitiveType primitiveType, ulong count)
    {
        if (primitiveType is PrimitiveType.Lines)
            FrameData.NewLineDraw(count);
        else if (primitiveType is PrimitiveType.Triangles)
            FrameData.NewTriangleDraw(count);
        else
            throw new NotImplementedException();
    }

    public abstract void Draw(PrimitiveType primitiveType, int vertexOrIndexOffset = 0, int vertexOrIndexCount = -1);
}
