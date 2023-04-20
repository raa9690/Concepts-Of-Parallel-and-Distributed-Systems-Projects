using System;
using System.Threading;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Net.Http;



namespace Messenger
{
    /// <summary>
    /// A text based messaging app that allows for secure messaging through  a ttui
    /// </summary>
    class TextBasedClient
    {
        static readonly String MYEMAIL = "raa9690@rit.edu";
        static readonly String SERVERADDRESS = "http://kayrun.cs.rit.edu:5000/";

        // client used for the client/server communication
        static readonly HttpClient client = new HttpClient();

        /// <summary>
        /// Main file that acts as the UI for the messaging service 
        /// </summary>
        /// <param name="args"> 
        /// argument for the ttui, usage:
        /// keyGen <keysize>
        ///     generate a public and private key of size keysize bits. the value is floored to the nearest multiple of 8,
        ///     as the methods used to generate prime numbers work under byte sizes. saves the public and private keys in
        ///     public.key and private.key files respectively.
        /// sendKey <email>
        ///     send your public key to the server under your email, this allows other users to get access to your public
        ///     key to encrypt messages to you, while keeping your private key hidden, also updates the key file with your
        ///     email.
        /// getKey <email>
        ///     get a public key of a user to be able to be able to send encrypted messages to the user, saves it in a file
        ///     with the format <email>.key
        /// sendMsg <email> <message>
        ///     send message securely of a user who you've already requested a public key for, if you have not requested the
        ///     key, the command fails with a message notifying you of your mistake
        /// getMsg <email>
        ///     get a message from a user, and decrypts it using your private key
        /// </param>
        static void Main(String[] args) {

            if (args.Length == 2 && args[0] == "keyGen")
            {
                // try parse int on the key, if it is a valid integer, generate keys, and update the public.key & private.key files
                int keySize = 0;
                if (!int.TryParse(args[1], out keySize))
                {
                    Console.WriteLine("First argument must be a valid integer\n"); // TODO still needs better message
                    return;
                }
                generateKeys(keySize);
                Console.WriteLine("Press any key and enter to close window")
                Console.ReadLine();
            }
            else if (args.Length == 2 && args[0] == "sendKey")
            {
                String email = args[1]
                sendKey(email);
                Console.WriteLine("Press any key and enter to close window")
                Console.ReadLine();

            }
            else if (args.Length == 2 && args[0] == "getKey") 
            {
                String email = args[1];
                getKey(email);
                Console.WriteLine("Press any key and enter to close window")
                Console.ReadLine();
            } 
            else if (args.Length == 3 && args[0] == "sendMsg") 
            { 
                String email = args[1];
                String message = args[2];
                sendMsg(email, message);
                Console.WriteLine("Press any key and enter to close window")
                Console.ReadLine();
            } 
            else if (args.Length == 2 && args[0] == "getMsg") 
            {
                String email = args[1];
                getMsg(email);
                Console.WriteLine("Press any key and enter to close window")
                Console.ReadLine();
            } 
            else
            {
                Console.WriteLine("Unknown Command, here are all valid commands: \nkeyGen <keysize>\r\n\tgenerate a public and private key of size keysize bits. the value is floored to the nearest multiple of 8,\r\n\tas the methods used to generate prime numbers work under byte sizes. saves the public and private keys in\r\n\tpublic.key and private.key files respectively.\r\nsendKey <email>\r\n\tsend your public key to the server under your email, this allows other users to get access to your public\r\n\tkey to encrypt messages to you, while keeping your private key hidden, also updates the key file with your\r\n\temail.\r\ngetKey <email>\r\n\tget a public key of a user to be able to be able to send encrypted messages to the user, saves it in a file\r\n\twith the format <email>.key\r\nsendMsg <email> <message>\r\n\tsend message securely of a user who you've already requested a public key for, if you have not requested the\r\n\tkey, the command fails with a message notifying you of your mistake\r\ngetMsg <email>\r\n\tget a message from a user, and decrypts it using your private key");
                Console.WriteLine("Press any key and enter to close window")
                Console.ReadLine();
                return;
            }
        }

        /// <summary>
        /// get a message from a user, and decrypts it using your private key
        /// </summary>
        /// <param name="email">email of the user</param>
        public async static void getMsg(String email) {

            PrivateKey myPrivateKey = getPrivateKey();
            Console.WriteLine(myPrivateKey.generateJson());

            if (myPrivateKey == null)
            {
                Console.WriteLine("Private Key Not Found");
                return;
            }

            try
            {
                using HttpResponseMessage response = await client.GetAsync(SERVERADDRESS + "Message/" + email);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                EncryptedMessage encryptedMessage = JsonSerializer.Deserialize<EncryptedMessage>(responseBody);

                String encryptedMessageContent = encryptedMessage.content;
                byte[] encryptedMessageContentBytes = Convert.FromBase64String(encryptedMessageContent);
                BigInteger encryptedMessageValue = new BigInteger(encryptedMessageContentBytes);
                BigInteger messageValue = BigInteger.ModPow(encryptedMessageValue, myPrivateKey.getPrivateKey(), myPrivateKey.getN());
                byte[] messageBytes = messageValue.ToByteArray();
                String messageString = Encoding.ASCII.GetString(messageBytes);

                Console.WriteLine(messageString);

            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e.Message);
                return;
            }


        }


        /// <summary>
        /// sendMsg <email> <message>
        /// send message securely of a user who you've already requested a public key for, if you have not requested the
        /// key, the command fails with a message notifying you of your mistake
        /// </summary>
        /// <param name="email">email of the person you want to send a message to</param>
        /// <param name="message">message to encrypt and send</param>
        public async static void sendMsg(String email, String message) {

            String filename = email + ".key";

            // define the format of the path
            string path = Environment.CurrentDirectory + "\\" + filename;
            if (!File.Exists(path))
            {
                Console.WriteLine("Please request a public key from the user <" + email + "> with the getKey <email> command");
                return;
            }

            // other users public key
            PublicKey otherUsersPublicKey = getMyPublicKey(filename);

            byte[] messageBytes = Encoding.ASCII.GetBytes(message);
            BigInteger messageBigInteger = new BigInteger(messageBytes);
            BigInteger encryptedMessage = BigInteger.ModPow(messageBigInteger, otherUsersPublicKey.getPublicKey(), otherUsersPublicKey.getN());
            byte[] encryptedMessageBytes = encryptedMessage.ToByteArray();
            String encryptedMessageBase64 = Convert.ToBase64String(encryptedMessageBytes);

            var encryptedMessageJson = new EncryptedMessage
            {
                email = email,
                content = encryptedMessageBase64
            };

            string jsonString = JsonSerializer.Serialize(encryptedMessageJson);

            try 
            {
                StringContent content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                using HttpResponseMessage response = await client.PutAsync(SERVERADDRESS + "Message/" + email, content);
                response.EnsureSuccessStatusCode();
            } 
            catch (HttpRequestException e) 
            { 
                Console.WriteLine(e.Message);
                return;
            }
        }

        /// <summary>
        /// getKey <email>
        ///     get a public key of a user to be able to be able to send encrypted messages to the user, saves it in a file
        ///     with the format <email>.key
        /// </summary>
        /// <param name="email">your email that acts as your user</param>
        public async static void getKey(String email) {
            // assuming the email is valid

            try
            {
                using HttpResponseMessage response = await client.GetAsync(SERVERADDRESS + "Key/" + email);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine("Response body: " + responseBody);
                PublicKey otherPersonsPublicKey = new PublicKey(JsonSerializer.Deserialize<SerializablePublicKey>(responseBody));
                otherPersonsPublicKey.writeToFile(email + ".key");
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e);
                return;
            }

        }

        ///  <summary>
        ///  sendKey<email>
        ///  send your public key to the server under your email, this allows other users to get access to your public
        ///  key to encrypt messages to you, while keeping your private key hidden, also updates the key file with your
        ///  email.
        ///  </summary>
        ///  <param name="email">email of the person whom you want to send your email to</param>
        public async static void sendKey(String email) {
            // assuming the email is valid
            PublicKey myPublicKey = getMyPublicKey();
            Console.WriteLine(myPublicKey.generateJson());

            if (myPublicKey == null)
            {
                Console.WriteLine("Public Key Not Found");
                return;
            }
            

            // set our email to email
            myPublicKey.setEmail(email);

            // send public key
            try
            {
                StringContent content = new StringContent(myPublicKey.generateJson(), Encoding.UTF8, "application/json");
                using HttpResponseMessage response = await client.PutAsync(SERVERADDRESS + "Key/" + email, content);
                response.EnsureSuccessStatusCode();
                // we don't really care about the response body for a put
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine(e);
                return;
            }
            // and update our public.key file if successful
            myPublicKey.writeToFile();
        }

        /// <summary>
        /// gets a public key from memory, defaults to your public key file if not specified
        /// </summary>
        /// <param name="filename">file name where we are looking for a public key, used to find both out public
        /// key, and the public key we save of other users
        /// </param>
        /// <returns>returns a public key object from the file</returns>
        public static PublicKey getMyPublicKey(String filename = "public.key") {
            Console.WriteLine("reading my public key");

            // define the format of the path
            string path = Environment.CurrentDirectory + "\\" + filename;
            if (!File.Exists(path))
            {
                return null;
            }

            // assuming the contents of the file are correct (that they aren't changed between runs)
            string jsonString = File.ReadAllText(path);
            SerializablePublicKey? serializablePublicKey =
                JsonSerializer.Deserialize<SerializablePublicKey>(jsonString);
            return new PublicKey(serializablePublicKey);

        }

        /// <summary>
        /// gets a private key from memory, defaults to your private key file if not specified
        /// </summary>
        /// <param name="filename">file name where we are looking for a private key
        /// </param>
        /// <returns>returns a private key object from the file</returns>
        public static PrivateKey getPrivateKey(String filename = "private.key") {
            Console.WriteLine("reading my private key");

            // define the format of the path
            string path = Environment.CurrentDirectory + "\\" + filename;
            if (!File.Exists(path))
            {
                return null;
            }

            // assuming the contents of the file are correct (that they aren't changed between runs)
            string jsonString = File.ReadAllText(path);
            SerializablePrivateKey? serializablePrivateKey =
                JsonSerializer.Deserialize<SerializablePrivateKey>(jsonString);
            return new PrivateKey(serializablePrivateKey);
        }

        /// <summary>
        /// generates public and private keys of a keySize, and saves their values in a public and private key files
        /// </summary>
        static void generateKeys(int keySize) {
            // keep keySize as a multiple of 8, since we use amount of bytes rather than bits
            keySize -= keySize % 8;
            int keyByteSize = keySize / 8;
            BigInteger[] pAndQ = genPQ(keyByteSize);
            BigInteger[] keyData = getKeys(pAndQ[0], pAndQ[1]);
            BigInteger N = keyData[0];
            BigInteger E = keyData[2];
            BigInteger D = keyData[3];

            writePublicKey(N, E);
            writePrivateKey(N, D);

            Console.ReadLine();
        }

        /// <summary>
        /// writes the necessary information to store a public key on file
        /// </summary>
        public static void writePublicKey(BigInteger N, BigInteger E) {
            PublicKey publicKey = new PublicKey(E, N);
            publicKey.writeToFile();
        }

        /// <summary>
        /// writes the necessary information to store a private key on file
        /// </summary>
        public static void writePrivateKey(BigInteger N, BigInteger D) {
            String[] emails = new String[0];
            PrivateKey privateKey = new PrivateKey(emails, D, N);
            privateKey.writeToFile();
        }

        /// <summary>
        /// we are testing for falseness, for a random selection of numbers
        /// to act as plaintext values, encrypt it, then decrypt it, then
        /// check if the decrypted plaintext is the same as the encrypted text
        /// returns false if any test failed, and prints out how many tests
        /// failed.
        /// </summary>
        /// 
        static bool testReversability(BigInteger E, BigInteger D, BigInteger N, int numOfTests = 1000) {
            int failedTestCount = 0;
            int count = 0;
            while (count < numOfTests)
            {
                BigInteger P = new BigInteger(RandomNumberGenerator.GetBytes(64));
                if (P != BigInteger.ModPow(BigInteger.ModPow(P, E, N), D, N))
                {
                    failedTestCount++;
                    Console.WriteLine("Failed");
                }
                count++;
            }

            if (failedTestCount == 0)
            {
                return true;
            }
            else
            {
                Console.WriteLine(failedTestCount);
                return false;
            }
        }

        static BigInteger[] getKeys(BigInteger p, BigInteger q) {
            BigInteger N = p * q;
            BigInteger r = (p - 1) * (q - 1);
            BigInteger E = generateProbablyPrime(2 ^ 16);
            BigInteger D = modInverse(E, r);
            BigInteger[] keyData = new BigInteger[4];
            keyData[0] = N; // modulus
            keyData[1] = r; // totient
            keyData[2] = E; // public key
            keyData[3] = D; // private key
            return keyData;
        }

        static BigInteger modInverse(BigInteger a, BigInteger n)
        {
            BigInteger i = n, v = 0, d = 1;
            while (a > 0)
            {
                BigInteger t = i / a, x = a;
                a = i % x;
                i = x;
                x = d;
                d = v - t * x;
                v = x;
            }
            v %= n;
            if (v < 0) v = (v + n) % n;
            return v;
        }

        /// <summary>
        /// Given a keyByte size, get a p and q primary number where pSize + qSize = keySize
        /// </summary>
        static BigInteger[] genPQ(int keyByteSize) {
            int pSize = keyByteSize / 2;
            /// determin weather the sign is positive or negative
            int sign = 1;
            if (RandomNumberGenerator.GetInt32(0, 2) > 0) {
                sign *= -1;
            }
            // rather than generating a float from 0 to 1, using a much simpler get into from 2000 to 3000, and dividing that by 10000
            // , the lost complexity shouldn't matter, since 8kbytes doesn't have over 10000 bytes, so the difference shouldn't be that big
            pSize = pSize + sign * ((RandomNumberGenerator.GetInt32(2000, 3000) / 10000) * pSize);
            pSize -= pSize % 8;
            int qSize = keyByteSize - pSize;
            BigInteger p = generateProbablyPrime(pSize);
            /*Console.Out.WriteLine("Generated p");*/
            BigInteger q = generateProbablyPrime(qSize);
            /*Console.Out.WriteLine("Generated q");*/
            BigInteger[] pAndQ = new BigInteger[2];
            pAndQ[0] = p;
            pAndQ[1] = q;
            return pAndQ;
        }

        /// <summary>
        /// Generates a prime number by looping generating a random number of 
        /// a given bytesize, testing if it's prime, if so, return it, else,
        /// continue the loop.
        /// </summary>
        /// <returns>A number very likely to be prime.</returns>
        static BigInteger generateProbablyPrime(int byteSize)
        {
            BigInteger possiblePrime = 0;
            do
            {
                // generate a random array of bytes
                // and garentee it's  greater than 1 (if less than or equal to 1, it'll never be prime)
                possiblePrime = new BigInteger(RandomNumberGenerator.GetBytes(byteSize));
                // simple cleanup of negative numbers
                if (possiblePrime < 0) possiblePrime = possiblePrime * -1;
            } while (!IsProbablyPrime(possiblePrime, byteSize));
            return possiblePrime;
        }

        static Boolean IsProbablyPrime(BigInteger value, int byteSize, int k = 10)
        {
            // simple check to confirm the value is an odd integer
            // includes simple checks for composite numbers (multiples of prime numbers 2, 3, 5, 7)
            // this is meant to impove performance
            if (value < 3 || value % 2 == 0
                || value % 3 == 0
                || value % 5 == 0
                || value % 7 == 0
                || value % 9 == 0
                || value % 11 == 0
                || value % 13 == 0
                || value % 17 == 0
                || value % 19 == 0)
            {
                return false;
            }

            // now setup the required values for the Miller-Rabin primality algorithm
            BigInteger n = value;
            // d starts as equal to n, then as we divide by 2, we check if d is odd
            BigInteger d = n;
            // as we divide d, find the largest odd value of d, with
            // r will be defined by n - 1 = 2^r * d where d is odd,
            // and both d and r are integers
            BigInteger r = 0;
            // do while loop as r > 0
            do
            {
                d = d / 2; // effectively factor out a 2, and increase the exponent r
                r++;
            } while (d % 2 == 0);

            // with n, d & r defined, find a
            BigInteger a = 0;
            while (a < 2 || a > n - 2)
            {
                // generate an a that is in the range [2, n - 2] (both inclusive)
                a = new BigInteger(RandomNumberGenerator.GetBytes(byteSize));
            }
            BigInteger x = 0;
            while (k > 0) // witness loop
            {
                // having a, d & n, generate x in the form: x = a^d mod n
                x = BigInteger.ModPow(a, d, n);

                // first escape check for current loop
                if (x == 1 || x == n - 1)
                {
                    k--;
                    continue;
                }

                // define rCounter to use as a counter for the internal while
                // loops while not changing r
                BigInteger rCounter = r;
                while (rCounter > 1) // r loop
                {
                    // x = x^2 mod n
                    x = BigInteger.ModPow(x, 2, n);
                    // if x ==  n-1 break out of the internal r loop
                    if (x == n - 1) break;
                    rCounter--;
                }
                // if we broke out of the r loop, then we continue the witness
                // loop, if not, return false (the number is composite) 
                if (x != n - 1)
                {
                    return false;
                }
                k--;
            }
            // if we reach the end of the witness loop without returning false
            // then assume the number is likely a prime
            return true;
        }
    };

    class EncryptedMessage
    {
        public String email { get; set; }
        public String content { get; set; }
    }

    class PublicKey
    {
        private String email;
        private BigInteger publicKey;
        private BigInteger N;

        public PublicKey(BigInteger publicKey, BigInteger N)
        {
            this.email = "";
            this.publicKey = publicKey;
            this.N = N;
        }

        public PublicKey(SerializablePublicKey serializablePublicKey)
        {
            this.email = serializablePublicKey.email;

            BigInteger[] nAndE = decodeKeyFromBase64(serializablePublicKey.key);
            this.publicKey = nAndE[1];
            this.N = nAndE[0];

        }

        public BigInteger getPublicKey() { return publicKey; }
        public BigInteger getN() { return N; } 
        public String getEmail() { return email; }

        public void setEmail(String email) { 
            this.email = email;
        }

        public void writeToFile(String filename = "public.key")
        {
            Console.WriteLine("writing public key");

            // define the format of the path
            string path = Environment.CurrentDirectory + "\\" + filename;
            // might be redundant, but a sanity check doesn't hurt
            Console.WriteLine("Writing to: " + path);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            String jsonString = generateJson();

            Console.WriteLine(jsonString);

            File.WriteAllText(path, jsonString);
            Console.WriteLine("Finished Writing");
        }

        public String encodeKeyToBase64() {
            byte[] nBytes = this.N.ToByteArray();
            int nSize = nBytes.Length;
            byte[] keyBytes = this.publicKey.ToByteArray();
            int keySize = keyBytes.Length;

            byte[] nSizeBytes = BitConverter.GetBytes(nSize);
            byte[] keySizeBytes = BitConverter.GetBytes(keySize);

            // flip the order of the arrays depending on the systems Endianness
            if (BitConverter.IsLittleEndian)
            {
                Console.WriteLine("Is Little Endian");
                Array.Reverse(nSizeBytes);
                Array.Reverse(keySizeBytes);
            }
            else
            {
                Console.WriteLine("Is Big Endian");
                Array.Reverse(nBytes);
                Array.Reverse(keyBytes);
            }

            // create the array of bytes to be encoded in Base 64
            byte[] keyInfoBytes = new byte[nBytes.Length + nSizeBytes.Length + keyBytes.Length + keySizeBytes.Length];
            int currentIndex = 0;
            for (int i = 0; i < nSizeBytes.Length; i++) {
                keyInfoBytes[currentIndex] = nSizeBytes[i];
                currentIndex++;
            }
            for (int i = 0; i < nBytes.Length; i++) {
                keyInfoBytes[currentIndex] = nBytes[i];
                currentIndex++;
            }
            for (int i = 0; i < keySizeBytes.Length; i++) {
                keyInfoBytes[currentIndex] = keySizeBytes[i];
                currentIndex++;
            }
            for (int i = 0; i < keyBytes.Length; i++) {
                keyInfoBytes[currentIndex] = keyBytes[i];
                currentIndex++;
            }

            // encode the string, and return it
            String base64EncodedKey = Convert.ToBase64String(keyInfoBytes);
            Console.WriteLine("For N of size: " + nSize.ToString());
            Console.WriteLine("For key of size: " + keySize.ToString());
            return base64EncodedKey;
        }

        public BigInteger[] decodeKeyFromBase64(String encodedKeyBase64) {
            int currentByte = 0;
            byte[] keyBodyBytes = Convert.FromBase64String(encodedKeyBase64);

            byte[] nSizeBytes = new byte[4];
            for (int i = currentByte; i < currentByte + 4; i++) {
                nSizeBytes[i - currentByte] = keyBodyBytes[i];
            }
            currentByte += nSizeBytes.Length; // always 4

            // if our system is big endian, flip the bytes
            if (BitConverter.IsLittleEndian) 
            {
                Array.Reverse(nSizeBytes);
            }
            int nSize = BitConverter.ToInt32(nSizeBytes);

            byte[] nBytes = new byte[nSize];
            for (int i = currentByte; i < currentByte + nSize; i++)
            {
                nBytes[i - currentByte] = keyBodyBytes[i];
            }
            currentByte += nSize;

            // if our system is little endian, flip the bytes
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(nBytes);
            }
            BigInteger N = new BigInteger(nBytes);

            byte[] keySizeBytes = new byte[4];
            for (int i = currentByte; i < currentByte + 4; i++)
            {
                keySizeBytes[i - currentByte] = keyBodyBytes[i];
            }
            currentByte += keySizeBytes.Length; // always 4

            // if our system is big endian, flip the bytes
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(keySizeBytes);
            }
            int keySize = BitConverter.ToInt32(keySizeBytes);

            byte[] keyBytes = new byte[nSize];
            for (int i = currentByte; i < currentByte + keySize; i++)
            {
                keyBytes[i - currentByte] = keyBodyBytes[i];
            }
            currentByte += keySize;

            // if our system is little endian, flip the bytes
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(keyBytes);
            }
            BigInteger E = new BigInteger(keyBytes);

            BigInteger[] nAndE = new BigInteger[2];
            nAndE[0] = N;
            nAndE[1] = E;
            return nAndE;

        }

        public String generateJson() {
            String keyBase64 = encodeKeyToBase64();
            var serializablePublicKey = new SerializablePublicKey
            {
                email = this.email,
                key = keyBase64
            };
            return JsonSerializer.Serialize(serializablePublicKey);
        }

    }

    class SerializablePublicKey {
        public String email { get; set; }
        public String key { get; set; }
    }

    class PrivateKey
    {
        private String[] emails;
        private BigInteger privateKey;
        private BigInteger N;

        public PrivateKey(String[] emails, BigInteger privateKey, BigInteger N)
        {
            this.emails = emails;
            this.privateKey = privateKey;
            this.N = N;
        }

        public PrivateKey(SerializablePrivateKey serializablePrivateKey)
        {
            this.emails = serializablePrivateKey.email;

            BigInteger[] nAndD = decodeKeyFromBase64(serializablePrivateKey.key);
            this.privateKey = nAndD[1];
            this.N = nAndD[0];

        }

        public BigInteger getPrivateKey() {
            return privateKey;
        }

        public BigInteger getN() { 
            return N; 
        }

        public void addEmail(String email) 
        {
            // if the email is already on the private key, do nothing
            String[] tempEmails = new String[this.emails.Length + 1];
            for (int i = 0; i < this.emails.Length; i++)
            {
                tempEmails[i] = this.emails[i];
            }
            tempEmails[this.emails.Length] = email;
            this.emails = tempEmails;
        }

        public void writeToFile(String filename = "private.key")
        {
            Console.WriteLine("writing private key");

            // define the format of the path
            string path = Environment.CurrentDirectory + "\\" + filename;
            // might be redundant, but a sanity check doesn't hurt
            Console.WriteLine("Writing to: " + path);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            String jsonString = generateJson();

            Console.WriteLine(jsonString);

            File.WriteAllText(path, jsonString);
            Console.WriteLine("Finished Writing");
        }

        public String encodeKeyToBase64()
        {
            byte[] nBytes = this.N.ToByteArray();
            int nSize = nBytes.Length;
            byte[] keyBytes = this.privateKey.ToByteArray();
            int keySize = keyBytes.Length;

            byte[] nSizeBytes = BitConverter.GetBytes(nSize);
            byte[] keySizeBytes = BitConverter.GetBytes(keySize);

            // flip the order of the arrays depending on the systems Endianness
            if (BitConverter.IsLittleEndian)
            {
                Console.WriteLine("Is Little Endian");
                Array.Reverse(nSizeBytes);
                Array.Reverse(keySizeBytes);
            }
            else
            {
                Console.WriteLine("Is Big Endian");
                Array.Reverse(nBytes);
                Array.Reverse(keyBytes);
            }

            // create the array of bytes to be encoded in Base 64
            byte[] keyInfoBytes = new byte[nBytes.Length + nSizeBytes.Length + keyBytes.Length + keySizeBytes.Length];
            int currentIndex = 0;
            for (int i = 0; i < nSizeBytes.Length; i++)
            {
                keyInfoBytes[currentIndex] = nSizeBytes[i];
                currentIndex++;
            }
            for (int i = 0; i < nBytes.Length; i++)
            {
                keyInfoBytes[currentIndex] = nBytes[i];
                currentIndex++;
            }
            for (int i = 0; i < keySizeBytes.Length; i++)
            {
                keyInfoBytes[currentIndex] = keySizeBytes[i];
                currentIndex++;
            }
            for (int i = 0; i < keyBytes.Length; i++)
            {
                keyInfoBytes[currentIndex] = keyBytes[i];
                currentIndex++;
            }

            // encode the string, and return it
            String base64EncodedKey = Convert.ToBase64String(keyInfoBytes);
            Console.WriteLine("For N of size: " + nSize.ToString());
            Console.WriteLine("For key of size: " + keySize.ToString());
            return base64EncodedKey;
        }

        public BigInteger[] decodeKeyFromBase64(String encodedKeyBase64)
        {

            int currentByte = 0;
            byte[] keyBodyBytes = Convert.FromBase64String(encodedKeyBase64);

            byte[] nSizeBytes = new byte[4];
            for (int i = currentByte; i < currentByte + 4; i++)
            {
                nSizeBytes[i - currentByte] = keyBodyBytes[i];
            }
            currentByte += nSizeBytes.Length; // always 4

            // if our system is big endian, flip the bytes
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(nSizeBytes);
            }
            int nSize = BitConverter.ToInt32(nSizeBytes);

            byte[] nBytes = new byte[nSize];
            for (int i = currentByte; i < currentByte + nSize; i++)
            {
                nBytes[i - currentByte] = keyBodyBytes[i];
            }
            currentByte += nSize;

            // if our system is little endian, flip the bytes
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(nBytes);
            }
            BigInteger N = new BigInteger(nBytes);

            byte[] keySizeBytes = new byte[4];
            for (int i = currentByte; i < currentByte + 4; i++)
            {
                keySizeBytes[i - currentByte] = keyBodyBytes[i];
            }
            currentByte += keySizeBytes.Length; // always 4

            // if our system is big endian, flip the bytes
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(keySizeBytes);
            }
            int keySize = BitConverter.ToInt32(keySizeBytes);

            byte[] keyBytes = new byte[nSize];
            for (int i = currentByte; i < currentByte + keySize; i++)
            {
                keyBytes[i - currentByte] = keyBodyBytes[i];
            }
            currentByte += keySize;

            // if our system is little endian, flip the bytes
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(keyBytes);
            }
            BigInteger E = new BigInteger(keyBytes);

            BigInteger[] nAndE = new BigInteger[2];
            nAndE[0] = N;
            nAndE[1] = E;
            return nAndE;
        }

        public String generateJson()
        {
            String keyBase64 = encodeKeyToBase64();
            var serializablePrivateKey = new SerializablePrivateKey
            {
                email = this.emails,
                key = keyBase64
            };
            return JsonSerializer.Serialize(serializablePrivateKey);
        }
    }

    class SerializablePrivateKey
    {
        public String[] email { get; set; }
        public String key { get; set; }
    }
};