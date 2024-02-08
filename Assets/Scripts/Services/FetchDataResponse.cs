using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO.Services
{
    public class FetchDataResponse<T>
    {
        bool succeeded;
        public bool Succeeded
        {
            get { return succeeded; }
        }
        T data;
        public T Data
        {
            get { return data; }
        }

        public FetchDataResponse(bool succeeded, T data)
        {
            this.succeeded = succeeded;
            this.data = data;
        }

    }

}
