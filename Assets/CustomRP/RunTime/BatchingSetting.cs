using UnityEngine;

[System.Serializable]
public struct BatchingSetting {
    public bool useDynamicBatching;
    public bool useSRPBatching;
    public bool useGPUInstancing;

    public BatchingSetting(bool useDynamicBatching, bool useSRPBatching, bool useGPUInstancing) {
        this.useDynamicBatching = useDynamicBatching;
        this.useSRPBatching = useSRPBatching;
        this.useGPUInstancing = useGPUInstancing;
    }
}
