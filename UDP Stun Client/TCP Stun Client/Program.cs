using STUN;
using STUN.Attributes;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace TCP_Stun_Client
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!STUNClient.TryParseHostAndPort("stun.stunprotocol.org:3478", out IPEndPoint stunEndPoint))
                throw new Exception("Failed to resolve STUN server address");
            //if (!STUNClient.TryParseHostAndPort("stun3.l.google.com:19302", out IPEndPoint stunEndPoint))
            //    throw new Exception("Failed to resolve STUN server address");


            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            IPEndPoint bindEndPoint = new IPEndPoint(IPAddress.Any, 7777);
            socket.Bind(bindEndPoint);

            socket.Connect(stunEndPoint);




            //Socket socket2 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //socket2.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //socket2.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            //IPEndPoint bindEndPoint2 = new IPEndPoint(IPAddress.Any, 64865);
            //socket2.Bind(bindEndPoint2);

            //socket2.Connect(stunEndPoint);








            var result = new STUNQueryResult(); // the query result
            var transID = STUNMessage.GenerateTransactionID(); // get a random trans id
            var message = new STUNMessage(STUNMessageTypes.BindingRequest, transID); // create a bind request

            result.Socket = socket;
            result.ServerEndPoint = stunEndPoint;
            result.NATType = STUNNATType.Unspecified;

            // send the request to server
            socket.SendTo(message.GetBytes(), stunEndPoint);
            // we set result local endpoint after calling SendTo,
            // because if socket is unbound, the system will bind it after SendTo call.
            result.LocalEndPoint = socket.LocalEndPoint as IPEndPoint;

            // wait for response

            if (!socket.Poll(1000 * 1000, SelectMode.SelectRead))
            {
                throw new Exception("Failed to read");
            }

            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

            byte[] buffer = new byte[1024 * 2];
            int bytesRead = 0;

            bytesRead = socket.ReceiveFrom(buffer, ref endPoint);

            var responseBuffer = buffer.Take(bytesRead).ToArray();
            var queryResult = Parse(responseBuffer, result, message, transID);
            Console.WriteLine("IP: {0}", queryResult.PublicEndPoint.Address);
            Console.WriteLine("Port: {0}", queryResult.PublicEndPoint.Port);
            while (true)
            {

            }
        }

        static STUNQueryResult Parse(byte[] responseBuffer, STUNQueryResult result, STUNMessage message, byte[] transID)
        {
            // didn't receive anything
            if (responseBuffer == null)
            {
                result.QueryError = STUNQueryError.Timedout;
                return result;
            }

            // try to parse message
            if (!message.TryParse(responseBuffer))
            {
                result.QueryError = STUNQueryError.BadResponse;
                return result;
            }

            // check trans id
            if (!STUNClient.ByteArrayCompare(message.TransactionID, transID))
            {
                result.QueryError = STUNQueryError.BadTransactionID;
                return result;
            }

            // finds error-code attribute, used in case of binding error
            var errorAttr = message.Attributes.FirstOrDefault(p => p is STUNErrorCodeAttribute)
                                                                        as STUNErrorCodeAttribute;

            // if server responsed our request with error
            if (message.MessageType == STUNMessageTypes.BindingErrorResponse)
            {
                if (errorAttr == null)
                {
                    // we count a binding error without error-code attribute as bad response (no?)
                    result.QueryError = STUNQueryError.BadResponse;
                    return result;
                }

                result.QueryError = STUNQueryError.ServerError;
                result.ServerError = errorAttr.Error;
                result.ServerErrorPhrase = errorAttr.Phrase;
                return result;
            }

            // return if receive something else binding response
            if (message.MessageType != STUNMessageTypes.BindingResponse)
            {
                result.QueryError = STUNQueryError.BadResponse;
                return result;
            }

            // not used for now.
            var changedAddr = message.Attributes.FirstOrDefault(p => p is STUNChangedAddressAttribute) as STUNChangedAddressAttribute;

            // find mapped address attribue in message
            // this attribue should present
            var mappedAddressAttr = message.Attributes.FirstOrDefault(p => p is STUNMappedAddressAttribute)
                                                                                as STUNMappedAddressAttribute;
            if (mappedAddressAttr == null)
            {
                result.QueryError = STUNQueryError.BadResponse;
                return result;
            }
            else
            {
                result.PublicEndPoint = mappedAddressAttr.EndPoint;
            }

            // stop querying and return the public ip if user just wanted to know public ip

            result.QueryError = STUNQueryError.Success;
            return result;

        }
    }
}
