"""Transport checks with a disposable fake Editor; never touches Unity or saves."""
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import threading
import time

SOURCE = Path(__file__).with_name("Invoke-UnityEditor.ps1")


def run_case(outcome):
    with tempfile.TemporaryDirectory(prefix="snowglow-editor-protocol-") as temp:
        root = Path(temp)
        helper = root / "Tools/EditorAutomation/Invoke-UnityEditor.ps1"
        helper.parent.mkdir(parents=True)
        shutil.copyfile(SOURCE, helper)
        logs = root / "Logs/EditorAutomation"
        logs.mkdir(parents=True)
        (logs / "result.json").write_text(json.dumps({"requestId": "old", "state": "complete"}))

        def editor():
            request = logs / "request.json"
            deadline = time.monotonic() + 15
            while not request.exists() and time.monotonic() < deadline:
                time.sleep(0.05)
            if not request.exists():
                return
            data = json.loads(request.read_text(encoding="utf-8-sig"))
            assert data["action"] == "compile"
            if outcome != "timeout":
                time.sleep(0.7)
                result = {"requestId": data["id"], "state": outcome}
                (logs / "result.json").write_text(json.dumps(result))

        thread = threading.Thread(target=editor)
        thread.start()
        result = subprocess.run(["powershell", "-NoProfile", "-File", str(helper),
                                 "-Action", "Compile", "-TimeoutSeconds", "3"],
                                capture_output=True, text=True, timeout=20)
        thread.join()
        assert (result.returncode == 0) == (outcome == "complete"), result.stdout + result.stderr
        if outcome == "timeout":
            assert (logs / "request.json").exists(), "Timeout must preserve pending request"
        print(outcome, "passed")


for case in ("complete", "failed", "timeout"):
    run_case(case)
print("Protocol checks passed; these are not native Unity acceptance.")
