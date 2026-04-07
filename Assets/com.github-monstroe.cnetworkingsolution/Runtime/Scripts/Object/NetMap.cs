using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CNetworkingSolution
{
    public class NetMap : MonoBehaviour
    {
        internal List<ClientObject> GetStartingClientObjects()
        {
            return gameObject.GetComponentsInChildren<ClientObject>(true).OrderBy(n => n.transform.GetSiblingIndex()).ToList();
        }
    }
}