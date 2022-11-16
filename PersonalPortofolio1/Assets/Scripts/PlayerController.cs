using Unity.Netcode;
using UnityEngine;

namespace HelloWorld
{
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] private float speed = 1;
        [SerializeField] private float cameraSpeed = 0.5f;
        private Rigidbody objectRigidBody;

        private Rigidbody copyRBNT;
        private Transform copyTransformNT;


        public NetworkVariable<Transform> NTtransform = new NetworkVariable<Transform>();
        public NetworkVariable<Rigidbody> NTrigidBody = new NetworkVariable<Rigidbody>();

        EndelessTerrain terrainGenerator;

        public override void OnNetworkSpawn()
        {
                objectRigidBody = GetComponent<Rigidbody>();
                copyRBNT = objectRigidBody;
                copyTransformNT = transform;
                //Debug.Log("copyTransform " + copyTransformNT.position);
                Move();
                terrainGenerator = FindObjectOfType<EndelessTerrain>();
                terrainGenerator.viewer = this.transform;
        }

        public void Move()
        {
            if (NetworkManager.Singleton.IsServer)
            {
                //Debug.Log("Test 1");
                var inputForce = AxisMovementInput();
                objectRigidBody.AddForce(inputForce);
                copyRBNT.AddForce(inputForce);
                NTrigidBody.Value = copyRBNT;

                var cameraMovement = CameraMouseMovement();
                transform.Rotate(cameraMovement);
                copyTransformNT.Rotate(cameraMovement);
                NTtransform.Value = copyTransformNT;
            }
            else
            {
                SubmitPositionRequestServerRpc();
            }
        }

        [ServerRpc]
        void SubmitPositionRequestServerRpc(ServerRpcParams rpcParams = default)
        {
            copyRBNT.AddForce(AxisMovementInput());
            NTrigidBody.Value = copyRBNT;
            copyTransformNT.Rotate(CameraMouseMovement());
            NTtransform.Value = copyTransformNT;
        }

        Vector3 AxisMovementInput()
        {
            //Debug.Log("Test 2");
            Vector3 rightForceInput = Input.GetAxis("Horizontal") * Time.deltaTime * transform.right * speed;
            //Debug.Log("RightForce" + rightForceInput);
            Vector3 forwardForceInput = Input.GetAxis("Vertical") * Time.deltaTime * transform.forward * speed;
            return rightForceInput + forwardForceInput;
        }

        Vector3 CameraMouseMovement()
        {
            return new Vector3(0, Input.GetAxis("Mouse X") * cameraSpeed, 0);
        }

        void Update()
        {
            if (this.transform.position.y < -1)
            {
                copyTransformNT.position = new Vector3(0, 75, 0);
                Debug.Log("copyTransform " + copyTransformNT.position);
                NTtransform.Value = copyTransformNT;
                Debug.Log(NTtransform.Value.position);
            }
            objectRigidBody = NTrigidBody.Value;
            transform.position = NTtransform.Value.position;
            //Debug.Log(NTtransform.Value.position);
            transform.rotation = NTtransform.Value.rotation;
        }
    }
}