using System.Collections.Generic;
using UnityEngine;

public struct InputData
{
    public int tick;
    public bool forward, backward, left, right, up;
}

public class SimManager : Singleton<SimManager>
{
    #region Serialize Field
    [Header("Object")]
    [SerializeField] Rigidbody objectRB;
    [SerializeField] int speed;

    [Header("Host")]
    [SerializeField] private int logSize = 50; 
    #endregion

    #region InputData
    public Queue<InputData> inputQueue;
    //Client ~ inputData
    //Server ~ log
    #endregion

    #region private
    int tick;
    bool isHost;
    bool isRunning;
    #endregion

    #region Public
    public void StartSim(bool ishost)
    {
        Debug.Log("StartSim");
        this.isHost = ishost;
        isRunning = true;
    }
    #endregion

    #region Override Method
    public override void AwakeFunc()
    {
        Physics.simulationMode = SimulationMode.Script;
        inputQueue = new Queue<InputData>();
        tick = 0;
    }
    #endregion

    #region Unity
    private void FixedUpdate()
    {
        if (!isRunning) return;

        if (isHost)
        {
            InputData input = UpdateTickData();
            if(inputQueue.Count >= logSize)
            {
                inputQueue.Dequeue();
            }
            inputQueue.Enqueue(input);

            ProcessTickData(ref input);
            Physics.Simulate(Time.fixedDeltaTime);
            tick++;

            Singleton<NetworkManager>.Instance.SendInputData(inputQueue);
        }

        else
        {
            while (inputQueue.Count > 0)
            {
                InputData input = inputQueue.Dequeue();

                Debug.Log($"now Tick = {tick} / data Tick = {input.tick}");

                if (tick > input.tick)
                {
                    continue;
                }
                else if (tick < input.tick)
                {
                    Debug.LogError($"Tick Loss! now Tick = {tick} / data Tick = {input.tick}");
                    Application.Quit();
                    return;
                }

                ProcessTickData(ref input);
                Physics.Simulate(Time.fixedDeltaTime);
                tick++;
            }
            SendLastTick();
        }
    }
    #endregion

    #region Private Method
    private void ProcessTickData(ref InputData input)
    {
        Vector3 tmp = new Vector3(
            (input.left ? -1:0) + (input.right?1:0),
            (input.up ? 10 : 0),
            (input.backward ? -1 : 0) + (input.forward ? 1 : 0)
            ) * speed; 
        
        if(tmp != Vector3.zero) { Debug.Log($"Input Data: {tmp}"); }
        objectRB.AddForce(tmp);
    }
    #endregion

    #region Host Method
    private InputData UpdateTickData()
    {
        return new InputData
        {
            tick = this.tick,
            forward = Input.GetKey(KeyCode.W),
            backward = Input.GetKey(KeyCode.S),
            left = Input.GetKey(KeyCode.A),
            right = Input.GetKey(KeyCode.D),
            up = Input.GetKey(KeyCode.Space)
        };
    }

    public void ClearLog(int lastClientTick)
    {
        int cnt = 0;
        foreach(var data in inputQueue)
        {
            if (data.tick > lastClientTick) break;
            cnt++;
        }
        for(int i = 0; i < cnt; i++)
        {
            inputQueue.Dequeue();
        }
    } 
    #endregion

    #region Client Method
    private void SendLastTick()
    {
        Singleton<NetworkManager>.Instance.SendClientLastTick(tick - 1);
        return;
    }
    #endregion


    

    

    
}
