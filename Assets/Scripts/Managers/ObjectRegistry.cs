using System.Collections.Generic;
using UnityEngine;

public class ObjectRegistry : MonoBehaviour {
    public static ObjectRegistry registry { get; private set; }

    Dictionary<string, List<GameObject>> _registeredObjects = new Dictionary<string, List<GameObject>>();

    void Awake() {
        if (registry == null) {
            registry = this;
        } else {
            Destroy(gameObject);
        }
    }

    public void Register(GameObject obj, string id) {
        if (!_registeredObjects.ContainsKey(id))
            _registeredObjects.Add(id, new List<GameObject>());

        _registeredObjects[id].Add(obj);
    }

    public List<GameObject> GetObjectList(string id) {
        if (_registeredObjects.TryGetValue(id, out List<GameObject> objectList))
                return _registeredObjects[id];
        return null;
    }
}
