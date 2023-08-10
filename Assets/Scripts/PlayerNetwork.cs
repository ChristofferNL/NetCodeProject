using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] Transform spawnedObjectPrefab;
    Transform spawnedObjectTransform;

    NetworkVariable<MyCustomData> randomNumber = new NetworkVariable<MyCustomData>(
        new MyCustomData
        {
            _int = 43,
            _bool = true,
        }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public struct MyCustomData : INetworkSerializable
    {
        public int _int;
        public bool _bool;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _int);
            serializer.SerializeValue(ref _bool);
        }
    }

    public override void OnNetworkSpawn()
    {
        randomNumber.OnValueChanged += (MyCustomData previousValue, MyCustomData newValue) =>
        {
            Debug.Log($"ID:{OwnerClientId} Int:{newValue._int} Bool:{newValue._bool}");
        };
    }

    private void Update()
    {
        if (!IsOwner) return;

        Vector3 moveDir = new Vector3(0, 0, 0);

        if (Input.GetKey(KeyCode.W)) moveDir.z = +1f;
        if (Input.GetKey(KeyCode.S)) moveDir.z = -1f;
        if (Input.GetKey(KeyCode.A)) moveDir.x = -1f;
        if (Input.GetKey(KeyCode.D)) moveDir.x = +1f;

        float moveSpeed = 3f;
        transform.position += moveDir * moveSpeed * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.T))
        {

            //if (IsHost)
            //{
            //    spawnedObjectTransform = Instantiate(spawnedObjectPrefab);
            //    spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true); // only server can use Spawn!
            //}

            /*  TestClientRpc(new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { 0, 1 } } });*/ // choose which clients receives the call
            TestServerRpc(new ServerRpcParams());
            //randomNumber.Value = new MyCustomData
            //{
            //    _int = UnityEngine.Random.Range(0, 100),
            //    _bool = !randomNumber.Value._bool,
            //};
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            if (spawnedObjectTransform != null)
            {
                //Destroy(spawnedObjectTransform.gameObject);
                spawnedObjectTransform.GetComponent<NetworkObject>().Despawn(false); // can despawn from the network but keep the object in the scene
            }
        }
    }

    [ServerRpc] // message to the server
    void TestServerRpc(ServerRpcParams serverRpcParams)
    {
        TestClientRpc("Blue", "Red", new ClientRpcParams());
        Debug.Log($"TestServerRpc {OwnerClientId} SenderClientID:{serverRpcParams.Receive.SenderClientId}");
    }

    [ClientRpc]
    void TestClientRpc(string colorOne, string colorTwo, ClientRpcParams clientRpcParams) // message from the server to the clients
    {
        Debug.Log($"TestServerRpc {OwnerClientId}");
        if (IsOwner)
        {
            FindObjectOfType<NetworkManagerUI>().SetTargetTexts(colorOne, colorTwo);
        }
        else
        {
            FindObjectOfType<NetworkManagerUI>().SetTargetTexts(colorTwo, colorOne);
        }
    }
}
