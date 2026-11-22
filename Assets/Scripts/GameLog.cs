namespace EchoShade
{
    /// <summary>
    /// Log thong tin cua game. Mac dinh BI LOAI KHOI BAN BUILD VA EDITOR (ke ca chuoi tham so),
    /// vi Debug.Log (lay stack trace + ve lai Console) gay khung hinh moi lan tua.
    /// Muon xem lai: them ECHOSHADE_LOG vao Project Settings > Player > Scripting Define Symbols.
    /// </summary>
    public static class GameLog
    {
        [System.Diagnostics.Conditional("ECHOSHADE_LOG")]
        public static void Info(string message) => UnityEngine.Debug.Log(message);
    }
}