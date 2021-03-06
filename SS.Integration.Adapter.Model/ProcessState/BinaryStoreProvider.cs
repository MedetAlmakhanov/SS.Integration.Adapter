//Copyright 2014 Spin Services Limited

//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at

//    http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using SS.Integration.Adapter.Model.Interfaces;

namespace SS.Integration.Adapter.Model.ProcessState
{
    public class BinaryStoreProvider<T> : FileStoreProvider, IObjectProvider<T>
    {
        private readonly string _pathFormatString;

        public BinaryStoreProvider(string directory, string pathFormatString)
            : base(directory)
        {
            _pathFormatString = pathFormatString;
        }

        private string GetPath(string id)
        {
            return GetFullPath(string.Format(_pathFormatString,id));
            //return string.Format(_pathFormatString, id);
        }

        public T GetObject(string id)
        {
            var filePath = GetPath(id);
            var serializedString = base.Read(filePath);
            if (serializedString == null)
                return default(T);

            var serializer = new BinaryFormatter();
            return (T)serializer.Deserialize(new MemoryStream(Convert.FromBase64String(serializedString)));
        }

        public void SetObject(string id, T item)
        {
            var filePath = GetPath(id);

            using (var memStream = new MemoryStream())
            {
                var serializer = new BinaryFormatter();
                serializer.Serialize(memStream, item);

                var serializedString = Convert.ToBase64String(memStream.ToArray());
                base.Write(filePath, serializedString);
            }

        }

        public bool Remove(string id)
        {
            var filePath = GetPath(id);
            if(File.Exists(filePath))
                File.Delete(filePath);
            return true;
        }

        public void Clear(string id = null)
        {
            if (!string.IsNullOrEmpty(id))
                Remove(id);
        }

        public void Flush()
        {

        }
    }
}
