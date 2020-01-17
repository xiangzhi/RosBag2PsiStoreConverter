using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Spatial.Euclidean;
using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;

namespace RosBagConverter.MessageSerializers
{
    public class GeometryMsgsSerializer : BaseMsgsSerializer
    {

        public GeometryMsgsSerializer(bool useHeaderTime = false, TimeSpan? offset = null)
            : base("geometry_msgs", useHeaderTime, offset: offset)
        {
        }

        private CoordinateSystem ConvertQuaternionToMatrix(Quaternion q, Point3D point)
        {
            var mat = Matrix<double>.Build.DenseIdentity(4);
            mat[0, 3] = point.X;
            mat[1, 3] = point.Y;
            mat[2, 3] = point.Z;

            q = q.Normalized;
            // convert quaternion to matrix
            mat[0, 0] = 1 - 2 * q.ImagY * q.ImagY - 2 * q.ImagZ * q.ImagZ;
            mat[0, 1] = 2 * q.ImagX * q.ImagY - 2 * q.ImagZ * q.Real;
            mat[0, 2] = 2 * q.ImagX * q.ImagZ + 2 * q.ImagY * q.Real;
            mat[1, 0] = 2 * q.ImagX * q.ImagY + 2 * q.ImagZ * q.Real;
            mat[1, 1] = 1 - 2 * q.ImagX * q.ImagX - 2 * q.ImagZ * q.ImagZ;
            mat[1, 2] = 2 * q.ImagY * q.ImagZ - 2 * q.ImagX * q.Real;
            mat[2, 0] = 2 * q.ImagX * q.ImagZ - 2 * q.ImagY * q.Real;
            mat[2, 1] = 2 * q.ImagY * q.ImagZ + 2 * q.ImagX * q.Real;
            mat[2, 2] = 1 - 2 * q.ImagX * q.ImagX - 2 * q.ImagY * q.ImagY;

            return new CoordinateSystem(mat);
        }

        public override bool SerializeMessage(Pipeline pipeline, Exporter store, string streamName, IEnumerable<RosMessage> messages, string messageType)
        {
            try
            {
                switch (messageType)
                {
                    case ("geometry_msgs/Quaternion"):
                        DynamicSerializers.WriteStronglyTyped<Quaternion>(pipeline, streamName, messages.Select(m =>
                        {
                            return (new Quaternion(m.GetField("w"), m.GetField("x"), m.GetField("y"), m.GetField("z")), m.Time.ToDateTime() + this.Offset);
                        }), store);
                        return true;
                    case ("geometry_msgs/Point"):
                        DynamicSerializers.WriteStronglyTyped<Point3D>(pipeline, streamName, messages.Select(m =>
                        {
                            return (new Point3D(m.GetField("x"), m.GetField("y"), m.GetField("z")), m.Time.ToDateTime() + this.Offset);
                        }), store);
                        return true;
                    case ("geometry_msgs/PointStamped"):
                        DynamicSerializers.WriteStronglyTyped<Point3D>(pipeline, streamName, messages.Select(m =>
                        {
                            var pointObject = m.GetField("point");
                            return (new Point3D(pointObject.GetField("x"), pointObject.GetField("y"), pointObject.GetField("z")), this.useHeaderTime ? ((RosHeader)m.GetField("header")).Time.ToDateTime() + this.Offset : m.Time.ToDateTime() + this.Offset);
                        }), store);
                        return true;
                    case ("geometry_msgs/Pose"):
                        DynamicSerializers.WriteStronglyTyped<CoordinateSystem>(pipeline, streamName, messages.Select(m =>
                        {
                            // get the orientation & Position
                            var ori = m.GetField("orientation");
                            var quaternion = new Quaternion(ori.GetField("w"), ori.GetField("x"), ori.GetField("y"), ori.GetField("z"));
                            var pos = m.GetField("position");
                            var point = new Point3D(pos.GetField("x"), pos.GetField("y"), pos.GetField("z"));

                            return (this.ConvertQuaternionToMatrix(quaternion, point), m.Time.ToDateTime() + this.Offset);
                        }), store);
                        return true;

                    case ("geometry_msgs/PoseStamped"):
                        DynamicSerializers.WriteStronglyTyped<CoordinateSystem>(pipeline, streamName, messages.Select(m =>
                        {
                            var p = m.GetField("Pose");
                            // get the orientation & Position
                            var ori = p.GetField("orientation");
                            var quaternion = new Quaternion(ori.GetField("w"), ori.GetField("x"), ori.GetField("y"), ori.GetField("z"));
                            var pos = p.GetField("position");
                            var point = new Point3D(pos.GetField("x"), pos.GetField("y"), pos.GetField("z"));
                            // TODO publish the header seperately
                            return (this.ConvertQuaternionToMatrix(quaternion, point), this.useHeaderTime ? ((RosHeader)m.GetField("header")).Time.ToDateTime() + this.Offset : m.Time.ToDateTime() + this.Offset);
                        }), store);
                        return true;

                    case ("geometry_msgs/Transform"):
                        DynamicSerializers.WriteStronglyTyped<CoordinateSystem>(pipeline, streamName, messages.Select(m =>
                        {
                            // get the orientation & Position
                            var ori = m.GetField("rotation");
                            var quaternion = new Quaternion(ori.GetField("w"), ori.GetField("x"), ori.GetField("y"), ori.GetField("z"));
                            var pos = m.GetField("translation");
                            var point = new Point3D(pos.GetField("x"), pos.GetField("y"), pos.GetField("z"));

                            return (this.ConvertQuaternionToMatrix(quaternion, point), m.Time.ToDateTime() + this.Offset);
                        }), store);
                        return true;

                    case ("geometry_msgs/Vector3"):
                        DynamicSerializers.WriteStronglyTyped<Vector3D>(pipeline, streamName, messages.Select(m =>
                        {
                            return (new Vector3D(m.GetField("x"), m.GetField("y"), m.GetField("z")), m.Time.ToDateTime() + this.Offset);
                        }), store);
                        return true;
                    default: return false;

                }
            }
            catch (NotSupportedException)
            {
                // Not supported default to total copy
                return false;
            }

        }
    }
}
