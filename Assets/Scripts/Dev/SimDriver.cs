using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace EchoShade.Dev
{
    /// <summary>
    /// CHI DE KIEM THU TU DONG: chay mot kich ban phim ("g 6; far; e; wait 2; ...") trong Play mode
    /// de chung minh tung man giai duoc. Khong co trong scene; script kiem thu tu them vao luc choi.
    /// Lenh: g T (di toi moc thoi gian T), x X (di toi toa do X), far, near, e, jump, r, wait S,
    /// waitghost (doi bong dung/het duong), log NHAN.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class SimDriver : MonoBehaviour
    {
        public static SimDriver Current;
        public static readonly StringBuilder Log = new StringBuilder();
        public static bool Done, Failed;
        public static string ShotDir = "";
        public string script;

        public static SimDriver Run(string script)
        {
            Log.Length = 0; Done = false; Failed = false;
            var go = new GameObject("SimDriver");
            DontDestroyOnLoad(go);
            var d = go.AddComponent<SimDriver>(); d.script = script; Current = d;
            return d;
        }

        void OnDestroy() { KhangInput.SimActive = false; KhangInput.SimHorizontal = 0f; }

        void Start() { KhangInput.SimActive = true; StartCoroutine(Go()); }

        static string F(float v) => v.ToString("0.00", CultureInfo.InvariantCulture);

        public static string State()
        {
            var c = LevelClock.Instance; var b = NhipBrain.Instance; var ph = PlayerHealth.Instance; var g = GhostPlayer.Instance;
            var k = ph ? ph.GetComponent<KhangController>() : null;
            var it = ph ? ph.GetComponent<KhangInteractor>() : null;
            return $"t={(c ? F(c.CurrentTime) : "-")} x={(k ? F(k.transform.position.x) : "-")} y={(k ? F(k.transform.position.y) : "-")} lane={(k ? k.CurrentLane.ToString() : "-")} " +
                   $"tier={(b ? b.Tier : -1)} rew={(c ? c.RewindCount : -1)} hp={(ph ? ph.HP : -1)} deaths={(ph ? ph.Deaths : -1)} held={(it && it.Held ? it.Held.name : "-")} " +
                   $"ghost={(g ? (g.Active ? (g.BlockedByAnchor ? "blocked" : g.Finished ? "finished" : "walking") + "@" + F(g.Position.x) : "off") : "none")} done={Mailbox.LevelCompleted}";
        }

        IEnumerator Go()
        {
            foreach (var raw in script.Split(';'))
            {
                var cmd = raw.Trim(); if (cmd.Length == 0) continue;
                var parts = cmd.Split(' ');
                string op = parts[0];
                float arg = parts.Length > 1 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var a) ? a : 0f;
                var ph = PlayerHealth.Instance; var k = ph ? ph.GetComponent<KhangController>() : null; var clock = LevelClock.Instance;
                bool needK = op == "g" || op == "x" || op == "far" || op == "near" || op == "jump";
                if (needK && (!k || !clock)) { Log.AppendLine("NO KHANG"); Failed = true; break; }
                float t0 = Time.time;
                switch (op)
                {
                    case "g":
                    case "x":
                        float tx = op == "g" ? clock.TimeToX(arg) : arg;
                        int deaths = ph.Deaths;
                        float dir0 = Mathf.Sign(tx - k.transform.position.x);
                        while ((tx - k.transform.position.x) * dir0 > 0.04f) // khong quay dau khi vuot qua (giu huong mat)
                        {
                            if (Mailbox.LevelCompleted) break;
                            if (ph.Deaths != deaths) { Log.AppendLine("  !! chet khi di: " + State()); deaths = ph.Deaths; }
                            KhangInput.SimHorizontal = dir0;
                            if (Time.time - t0 > 25f) { Log.AppendLine("  !! ket khi di toi " + F(tx) + ": " + State()); Failed = true; break; }
                            yield return null;
                        }
                        KhangInput.SimHorizontal = 0f;
                        yield return null;
                        break;
                    case "far":
                    case "near":
                        KhangInput.SimPress(op);
                        yield return null; yield return null;
                        while (k.IsSwitchingLane && Time.time - t0 < 3f) yield return null;
                        yield return new WaitForSeconds(0.05f);
                        break;
                    case "e": KhangInput.SimPress("interact"); yield return new WaitForSeconds(0.25f); break;
                    case "jump": KhangInput.SimPress("jump"); yield return new WaitForSeconds(0.1f); while ((k.IsVaulting || !k.GetComponent<CharacterController>().isGrounded) && Time.time - t0 < 3f) yield return null; yield return new WaitForSeconds(0.1f); break;
                    case "r": KhangInput.SimPress("restart"); yield return new WaitForSeconds(0.5f); break;
                    case "wait": yield return new WaitForSeconds(arg); break;
                    case "waitghost":
                        while (Time.time - t0 < 40f)
                        {
                            var g = GhostPlayer.Instance;
                            if (!g || !g.Active || g.Finished || g.BlockedByAnchor) break;
                            yield return null;
                        }
                        break;
                    case "log": break;
                    case "mem": { var b = NhipBrain.Instance; Log.AppendLine("  mem " + parts[1] + " = " + (b ? b.RecallLane(parts[1], -1).ToString() : "-") + " entries=" + (b ? b.memory.Count : 0)); break; }
                    case "shot":
                        if (!string.IsNullOrEmpty(ShotDir) && parts.Length > 1)
                        {
                            System.IO.Directory.CreateDirectory(ShotDir);
                            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(ShotDir, parts[1] + ".png"));
                        }
                        yield return new WaitForSeconds(0.4f);
                        break;
                    default: Log.AppendLine("?? " + cmd); break;
                }
                Log.AppendLine($"[{F(Time.time)}] {cmd,-10} | {State()}");
                if (Failed) break;
            }
            KhangInput.SimActive = false;
            Done = true;
        }
    }
}
