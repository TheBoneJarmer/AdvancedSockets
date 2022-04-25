using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AdvancedSockets.WebSockets
{
    public class WebSocketMessage
    {
        private byte[] buffer;

        public WebSocketOpcode Opcode { get; private set; }
        public byte[] Message { get; private set; }
        public bool Masked { get; private set; }
        internal bool Incomplete { get; private set; }

        public WebSocketMessage(byte[] buffer)
        {
            if (buffer == null)
            {
                this.buffer = new byte[0];
            }
            else
            {
                this.buffer = buffer;
            }
        }
        public WebSocketMessage(byte[] message, WebSocketOpcode opcode, bool masked)
        {
            Opcode = opcode;
            Masked = masked;

            if (message == null)
            {
                Message = new byte[0];
            }
            else
            {
                Message = message;
            }
        }

        public byte[] Decode()
        {
            int byteIndex = 0;

            // Primary byte
            var headerBytes = buffer[byteIndex].ByteToBinaryByteArray();
            var finished = headerBytes[0];
            var reserved1 = headerBytes[1];
            var reserved2 = headerBytes[2];
            var reserved3 = headerBytes[3];
            
            Opcode = (WebSocketOpcode)headerBytes.Slice(4, 4).BinaryByteArrayToByte();
            byteIndex += 1;

            // Disect the second byte
            var propertyByte = buffer[byteIndex].ByteToBinaryByteArray();
            var payloadLengthValue = propertyByte.Slice(1, 7).BinaryByteArrayToInt();
            var payloadLength = 0;

            // Set the public masked value
            Masked = Convert.ToBoolean(propertyByte[0]);

            // Get the payload length
            if (payloadLengthValue < 126)
            {
                payloadLength = payloadLengthValue;
                byteIndex += 1;
            }
            if (payloadLengthValue == 126)
            {
                string value1 = buffer[byteIndex + 1].ByteToBinaryString();
                string value2 = buffer[byteIndex + 2].ByteToBinaryString();

                payloadLength = (value1 + value2).BinaryStringToInt();
                byteIndex += 3;
            }
            if (payloadLengthValue == 127)
            {
                string value1 = buffer[byteIndex + 1].ByteToBinaryString();
                string value2 = buffer[byteIndex + 2].ByteToBinaryString();
                string value3 = buffer[byteIndex + 3].ByteToBinaryString();
                string value4 = buffer[byteIndex + 4].ByteToBinaryString();

                payloadLength = (value1 + value2 + value3 + value4).BinaryStringToInt();
                byteIndex += 5;
            }

            // Check if we are missing parts of the message
            // If so, mark the message as incomplete
            if (Masked && payloadLength + byteIndex + 4 > buffer.Length)
            {
                Incomplete = true;
                return buffer;
            }
            if (!Masked && payloadLength + byteIndex > buffer.Length)
            {
                Incomplete = true;
                return buffer;
            }

            if (Masked)
            {
                byte[] maskBytes = new byte[4];
                maskBytes[0] = buffer[byteIndex + 0];
                maskBytes[1] = buffer[byteIndex + 1];
                maskBytes[2] = buffer[byteIndex + 2];
                maskBytes[3] = buffer[byteIndex + 3];

                byteIndex += 4;

                // Payload bytes
                Message = new byte[payloadLength];

                for (int i = 0; i < payloadLength; i++)
                {
                    byte encoded = buffer[byteIndex + i];
                    byte decoded = (byte)(encoded ^ maskBytes[i % 4]);

                    Message[i] = decoded;
                }
            }
            else
            {
                // Payload bytes
                Message = new byte[payloadLength];

                for (int i = 0; i < payloadLength; i++)
                {
                    Message[i] = buffer[byteIndex + i];
                }
            }

            if (buffer.Length > byteIndex + payloadLength)
            {
                int index = byteIndex + payloadLength;
                int length = buffer.Length - index;

                return buffer.Slice(index, length);
            }

            return new byte[0];
        }
        public byte[] Encode()
        {
            var result = new List<byte>();

            // Generate the header byte aka the first byte
            var opcode = ((byte)Opcode).ByteToBinaryString().Substring(4);
            var header = "1000" + opcode;

            result.Add(header.BinaryStringToByte());

            // Generate the second byte aka the one containing the masked value and the payload length
            var maskedByte = Convert.ToByte(Masked);
            var msgLengthBinary = Message.Length.IntToBinaryString();

            // If the buffer length is smaller than 126 bytes the length itself converted to binary will be used as payload length value
            // Important to know is that the first bit represents the value of masked and the remaining 7 bits will form the value of the payload length
            if (Message.Length < 126)
            {
                result.Add((maskedByte + msgLengthBinary.Substring(1)).BinaryStringToByte());
            }

            // If the buffer length exceeds 126 bytes the protocol expects us to add 3 specific bytes
            // The first byte will be like the one above, but instead of using the buffer length as value we use the number 126 as value
            // In this case this means our first byte will be either "11111110" or "01111110", where the first bit of the binary sequence is the masked value
            // The second and third byte will together form the binary string of the actual buffer length
            // For example, lets say our buffer is 22851 bytes long. In that case our binary string looks like this: 0101 1001 0100 0011
            // However, as you might realise a byte is just 8 bits so we got to divide this binary string in two bytes.
            // And thus that would become our second and third byte
            if (Message.Length >= 126 && Message.Length < 65535)
            {
                result.Add((maskedByte + "1111110").BinaryStringToByte());
                result.Add(msgLengthBinary.PadLeft(16, '0').Substring(0, 8).BinaryStringToByte());
                result.Add(msgLengthBinary.PadLeft(16, '0').Substring(8, 8).BinaryStringToByte());
            }

            // In some case the buffer length is even so big it exceeds 65535 bytes. In that case we apply the same method as above
            // But instead of 2 bytes representing the payload length we use 4 bytes and instead of 126 we use 127 as indicator
            if (Message.Length >= 65535)
            {
                result.Add((maskedByte + "1111111").BinaryStringToByte());
                result.Add(msgLengthBinary.PadLeft(32, '0').Substring(0, 8).BinaryStringToByte());
                result.Add(msgLengthBinary.PadLeft(32, '0').Substring(8, 8).BinaryStringToByte());
                result.Add(msgLengthBinary.PadLeft(32, '0').Substring(16, 8).BinaryStringToByte());
                result.Add(msgLengthBinary.PadLeft(32, '0').Substring(24, 8).BinaryStringToByte());
            }

            if (Message.Length > 0)
            {
                if (Masked)
                {
                    Random random = new Random();
                    byte[] maskBytes = new byte[4];

                    // Generate the maskbytes according to the protocol
                    // In this case it means 4 random values which we use to encode the buffer bytes
                    maskBytes[0] = (byte)random.Next(0, 255);
                    maskBytes[1] = (byte)random.Next(0, 255);
                    maskBytes[2] = (byte)random.Next(0, 255);
                    maskBytes[3] = (byte)random.Next(0, 255);

                    result.AddRange(maskBytes);

                    // Now encode each byte in the buffer and add it to the result
                    for (int i = 0; i < Message.Length; i++)
                    {
                        result.Add((byte)(Message[i] ^ maskBytes[i % 4]));
                    }
                }
                else
                {
                    result.AddRange(Message);
                }
            }

            return result.ToArray();
        }
    }
}