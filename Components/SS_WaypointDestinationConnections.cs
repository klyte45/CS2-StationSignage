using Colossal.Serialization.Entities;
using Unity.Entities;

namespace StationSignage.Components
{
    public struct SS_WaypointDestinationConnections : IBufferElementData, ISerializable
    {
        public Entity line;
        public uint requestFrame;

        private const uint CURRENT_VERSION = 0;
        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > CURRENT_VERSION)
            {
                throw new System.Exception($"Unsupported version {version} for SS_WaypointDestinationConnections.");
            }
            reader.Read(out line);
            reader.Read(out requestFrame);
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(CURRENT_VERSION);
            writer.Write(line);
            writer.Write(requestFrame);
        }
    }
}
