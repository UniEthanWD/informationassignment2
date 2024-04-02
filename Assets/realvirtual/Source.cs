// realvirtual (R) Framework for Automation Concept Design, Virtual Commissioning and 3D-HMI
// (c) 2019 realvirtual GmbH - Usage of this source code only allowed based on License conditions see https://realvirtual.io/en/company/license

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NaughtyAttributes;
#if REALVIRTUAL_AGX
using AGXUnity;
#endif
using Mesh = UnityEngine.Mesh;
using Random = System.Random;

namespace realvirtual
{
    [System.Serializable]
    public class realvirtualEventMUCreated: UnityEvent<MU>
    {
    }
    
    [SelectionBase]
    //! The Source is generating MUs during simulation time.
    //! The Source is generating new MUs based on the referenced (ThisObjectAsMU) GameObject. 
    //! When generating an MU a copy of the referenced GameObject will be created.
    [HelpURL("https://doc.realvirtual.io/components-and-scripts/source")]
    public class Source : BaseSource,ISignalInterface
    {
        // Public / UI Variablies
        #if REALVIRTUAL_AGX
        public bool UseAGXPhysics;
        #else
        [HideInInspector] public bool UseAGXPhysics=false;
        #endif
        [Header("General Settings")] public GameObject ThisObjectAsMU; //!< The referenced GameObject which should be used as a prototype for the MU. If it is null it will be this GameObject.
        public GameObject Destination; //!< The destination GameObject where the generated MU should be placed
        public bool Enabled = true; //!< If set to true the Source is enabled
        public bool FreezeSourcePosition = true; //!< If set to true the Source itself (the MU template) is fixed to its position
        public bool DontVisualize = true; //!< True if the Source should not be visible during Simulation time
        public float Mass = 1; //!< Mass of the generated MU.
        public bool SetCenterOfMass = false;
        public Vector3 CenterOfMass = new Vector3(0,0,0); //!< Mass of the generated MU.
        public string GenerateOnLayer =""; //! Layer where the MUs should be generated to - if kept empty no layers are changed
        [HideInInspector] public bool ChangeDefaultLayer = true;  //! If set to true Layers are automatically changed if default Layer is detected
        [ReorderableList] public List<string> OnCreateDestroyComponents = new List<string>(); //! Destroy this components on MU when MU is created as a copy of the source - is used to delete additional source scripts
        [Header("Create in Intverval (0 if not)")]
        public float StartInterval = 0; //! Start MU creation with the given seconds after simulation start
        public float Interval = 0; //! Interval in seconds between the generation of MUs. Needs to be set to 0 if no interval generation is wished.

        [Header("Automatic Generation on Distance")]
        public bool AutomaticGeneration = true; //! Automatic generation of MUs if last MU is above the GenerateIfDistance distance from MU
        [ShowIf("AutomaticGeneration")]public float GenerateIfDistance = 300; //! Distance in millimeters from Source when new MUs should be generated.
        [ShowIf("AutomaticGeneration")]public bool RandomDistance = false; //! If turned on Distance is Random Number with plus / minus Range Distance
        [ShowIf("RandomDistance")]public float RangeDistance = 300;  //! Range of the distance (plus / minus) if RandomDistance is turned on
        [Header("Number of MUs")] public bool LimitNumber = false;
        [ShowIf("LimitNumber")]public int MaxNumberMUs = 1;
        [ShowIf("AutomaticGeneration")][ReadOnly]public int Created = 0;
        
        [Header("Source IO's")] public bool GenerateMU=true; //! When changing from false to true a new MU is generated.
        public bool DeleteAllMU; //! when changing from false to true all MUs generated by this Source are deleted.

        [Header("Source Signals")] public PLCOutputBool SourceGenerate; //! When changing from false to true a new MU is generated.

        [Header("Events")] public realvirtualEventMUCreated
            EventMUCreated;

        [HideInInspector] public bool PositionOverwrite = false;
        // Private Variablies
        private bool _generatebefore = false;
        private bool _deleteallmusbefore = false;
        private bool _tmpoccupied;
        private GameObject _lastgenerated;
        private int ID = 0;
        private bool _generatenotnull = false;
        private List<GameObject> _generated = new List<GameObject>();
        private float nextdistance;
        
        //! Deletes all MU generated by this Source
        public void DeleteAll()
        {
            foreach (GameObject obj in _generated)
            {
                Destroy(obj);
            }

            _generated.Clear();
        }
        
        //! Deletes all MU generated by this Source
        public void DeleteAllImmediate()
        {
            foreach (GameObject obj in _generated)
            {
                DestroyImmediate(obj);
            }

            _generated.Clear();
        }
        
        
        protected void Reset()
        {
            if (ThisObjectAsMU == null)
            {
                ThisObjectAsMU = gameObject;
            }
        }

        void GenerateInterval()
        {
            if (!PositionOverwrite)
                Generate();
        }

        protected void Start()
        {
       
            if (SourceGenerate != null)
                _generatenotnull = true;
            
            if (ThisObjectAsMU == null)
            {
                ErrorMessage("Object to be created needs to be defined in [This Object As MU]");
            }

            if (ThisObjectAsMU != null)
            {
                if (ThisObjectAsMU.GetComponent<MU>() == null)
                {
                    ThisObjectAsMU.AddComponent<MU>();
                }
            }

            if (Interval > 0)
            {
                InvokeRepeating("GenerateInterval", StartInterval, Interval);
            }

            // Don't show source and don't collide - source is just a blueprint for generating the MUs
            SetVisibility(!DontVisualize);
            SetCollider(false);
            SetFreezePosition(FreezeSourcePosition);
#if REALVIRTUAL_AGX
            if (UseAGXPhysics)
            {
                var rbodies = GetComponentsInChildren<RigidBody>();
                foreach (var rbody in rbodies)
                {
                    rbody.enabled = false;
                }
            }
#endif

            if (GetComponent<Collider>() != null)
            {
                GetComponent<Collider>().enabled = false;
            }
            
            // Deactivate all fixers if included in Source
            var fixers = GetComponentsInChildren<IFix>();
            foreach (var fix in fixers)
            {
                fix.DeActivate(true);
            }

            nextdistance = GenerateIfDistance;
        }

        
        //! For Layout Editor mode Start  is called when the simulation is started
        protected override void OnStart()
        {
            SetVisibility(false);
          
        }
        
        //! For Layout Editor mode Stop  is called when the simulation is stopped
        protected override void OnStop()
        {
            Invoke("DelayOnStop",0.1f);
            
        }

        private void DelayOnStop()
        {
            SetVisibility(true);
        }

        
        private void FixedUpdate()
        {
            // Delete  on Keypressed
            if (Input.GetKeyDown(realvirtualController.HotkeyDelete))
            {
                if (realvirtualController.EnableHotkeys)
                    DeleteAll();
            }
            if (PositionOverwrite)
                return;
            
            if (_generatenotnull)
                GenerateMU = SourceGenerate.Value;

            // Generate on Signal Genarate MU
            if (_generatebefore != GenerateMU)
            {
                if (GenerateMU)
                {
                    _generatebefore = GenerateMU;
                    Generate();
                }
            }

            // Generate if Distance
            if (AutomaticGeneration)
            {
                if (_lastgenerated != null)
                {
                    float distance = Vector3.Distance(_lastgenerated.transform.position, gameObject.transform.position) *
                                     realvirtualController.Scale;

                    if (distance > nextdistance)
                    {
                        Generate();
                        if (RandomDistance)
                        {
                            float random = UnityEngine.Random.Range(-RangeDistance, RangeDistance);
                            nextdistance = GenerateIfDistance + random;
                        }
                    }
                }
            }

            // Generate on Keypressed
            if (Input.GetKeyDown(realvirtualController.HotkeyCreateOnSource))
            {
                Generate();
            }

            if (GenerateMU == false)
            {
                _generatebefore = false;
            }

            if (DeleteAllMU != _deleteallmusbefore && DeleteAllMU == true)
            {
                DeleteAll();
            }
            
            _deleteallmusbefore = DeleteAllMU;
        }

        //! Generates an MU.
        public MU Generate()
        {

#if !REALVIRTUAL_AGX
          UseAGXPhysics = false;
#endif
            if (LimitNumber && (Created >= MaxNumberMUs))
                return null;
            
            if (Enabled)
            {
                GameObject newmu = GameObject.Instantiate(ThisObjectAsMU, transform.position, transform.rotation);
                if (GenerateOnLayer != "")
                {
                    if (LayerMask.NameToLayer(GenerateOnLayer) != -1)
                    {
                        newmu.layer = LayerMask.NameToLayer(GenerateOnLayer);
                    }
                }

                if (ChangeDefaultLayer)
                {
                    /// Check if still default layer -- if yes then set box collider to g4a MU
                    var box = newmu.GetComponentInChildren<BoxCollider>();
                    if (box != null)
                    {
                        if (box.gameObject.layer == LayerMask.NameToLayer("Default"))
                            box.gameObject.layer = LayerMask.NameToLayer("rvMU");
                    }

                    var mesh = newmu.GetComponentInChildren<MeshCollider>();
                    if (mesh != null)
                    {
                        if (mesh.gameObject.layer == LayerMask.NameToLayer("Default"))
                            mesh.gameObject.layer = LayerMask.NameToLayer("rvMUTransport");
                    }
                }

                Source source = newmu.GetComponent<Source>();

                Created++;
                if (!UseAGXPhysics)
                {
                    Rigidbody newrigid = newmu.GetComponentInChildren<Rigidbody>();
                    if (newrigid == null)
                    {
                        newrigid = newmu.AddComponent<Rigidbody>();
                    }
                
                    newrigid.mass = Mass;
                    
                    Collider collider = newmu.GetComponentInChildren<Collider>();
                    BoxCollider newboxcollider;
                    if (collider == null)
                    {
                        newboxcollider = newmu.AddComponent<BoxCollider>();
                        MeshFilter mumsmeshfilter = newmu.GetComponentInChildren<MeshFilter>();
                        Mesh mumesh = mumsmeshfilter.mesh;
                        GameObject obj = mumsmeshfilter.gameObject;
                        if (mumesh != null)
                        {
                            Vector3 globalcenter = obj.transform.TransformPoint(mumesh.bounds.center);
                            Vector3 globalsize = obj.transform.TransformVector(mumesh.bounds.size);
                            newboxcollider.center = newmu.transform.InverseTransformPoint(globalcenter);
                            Vector3 size = newmu.transform.InverseTransformVector(globalsize);
                            if (size.x < 0)
                            {
                                size.x = -size.x;
                            }

                            if (size.y < 0)
                            {
                                size.y = -size.y;
                            }

                            if (size.z < 0)
                            {
                                size.z = -size.z;
                            }

                            newboxcollider.size = size;
                        }
                    }
                    else
                    {
                      //  newboxcollider.enabled = true;
                    }
                    newrigid.mass = Mass;
                    if (SetCenterOfMass)
                        newrigid.centerOfMass = CenterOfMass;
                }
                else
                {
#if REALVIRTUAL_AGX
                    // Enable AGX Rigidbodies when newmu is created
                    var rbodies = newmu.GetComponentsInChildren<RigidBody>();
                        foreach (var rbody in rbodies)
                        {
                            rbody.enabled = true;
                        }
#endif
                }

                if (source != null)
                {
                    source.SetVisibility(true);
                    source.SetCollider(true);
                    source.SetFreezePosition(false);
                    source.Enabled = false;
                    source.enabled = false;
                }

                ID++;
                MU mu = newmu.GetComponent<MU>();
                if (Destination != null)
                {
                    newmu.transform.parent = Destination.transform;
                }
                
             
            
                if (mu == null)
                {
                    ErrorMessage("Object generated by source need to have MU script attached!");
                }
                else
                {
                    mu.InitMu(name,ID,realvirtualController.GetMUID(newmu));
                }
                
                mu.CreatedBy = this;

                DestroyImmediate(source);
                // Destroy Additional Components
                foreach (var componentname in OnCreateDestroyComponents)
                {
                    
                    Component[] components = newmu.GetComponents(typeof(Component));
                    foreach(Component component in components)
                    {
                        var ty = component.GetType();
                        if (ty.ToString()==componentname)    
                            Destroy(component);
                    }
                }
                
                // Activate all Fixers if included
                var fixers = mu.GetComponentsInChildren<IFix>();
                foreach (var fix in fixers)
                {
                    fix.DeActivate(false);
                }
            
                _lastgenerated = newmu;
                _generated.Add(newmu);
                EventMUCreated.Invoke(mu);
                var isources = newmu.GetComponents<ISourceCreated>();
                foreach (var isource in isources)
                {
                    isource.OnSourceCreated();
                }
                return mu;
            }

            return null;
        }

    }
}