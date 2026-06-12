import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

WORKER = Path(__file__).resolve().parents[1] / "reva_docling_worker" / "main.py"


class WorkerTests(unittest.TestCase):
    def test_parse_text_statement(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            sample = Path(directory) / "statement.txt"
            sample.write_text("Cedent: Andes Mutual\nBroker: Meridian Re\nCurrency: USD\nPremium: 120000", encoding="utf-8")
            result = subprocess.run([sys.executable, str(WORKER), "--input", str(sample)], capture_output=True, text=True, check=True)
            payload = json.loads(result.stdout)
            self.assertEqual(payload["parserProfile"], "fallback-text")
            self.assertIn("Andes Mutual", payload["text"])

    def test_parse_csv_bordereau(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            sample = Path(directory) / "bordereau.csv"
            sample.write_text("Cedent,Broker,Currency,Premium\nAndes Mutual,Meridian Re,USD,120000", encoding="utf-8")
            result = subprocess.run([sys.executable, str(WORKER), "--input", str(sample)], capture_output=True, text=True, check=True)
            payload = json.loads(result.stdout)
            self.assertEqual(payload["tables"][0]["rows"][0]["Cedent"], "Andes Mutual")


if __name__ == "__main__":
    unittest.main()
