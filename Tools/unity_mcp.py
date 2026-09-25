"""Minimal MCP streamable-HTTP client for the local MCPForUnity server.

Usage:
  python unity_mcp.py list-tools
  python unity_mcp.py call <tool_name> '<json_args>'
  python unity_mcp.py menu "Detective/Build Core And Regions"   (fire-once, no retry)
  python unity_mcp.py resource "mcpforunity://scene/gameobject/<id>"
"""
import json
import sys
import time
import uuid

import requests

URL = "http://127.0.0.1:8080/mcp"
HEADERS = {
    "Content-Type": "application/json",
    "Accept": "application/json, text/event-stream",
}


def parse_sse_events(resp):
    """Return all JSON events in an SSE or plain-JSON response body."""
    text = resp.text
    events = []
    data_lines = []
    for line in text.splitlines():
        if line.startswith("data:"):
            data_lines.append(line[5:].strip())
        elif line.strip() == "" and data_lines:
            try:
                events.append(json.loads("\n".join(data_lines)))
            except json.JSONDecodeError:
                pass
            data_lines = []
    if data_lines:
        try:
            events.append(json.loads("\n".join(data_lines)))
        except json.JSONDecodeError:
            pass
    if not events and text.strip():
        try:
            events.append(json.loads(text))
        except json.JSONDecodeError:
            pass
    return events


class MCP:
    def __init__(self):
        self.session = None
        self._init()

    def _post(self, payload, want_id=None):
        h = dict(HEADERS)
        if self.session:
            h["Mcp-Session-Id"] = self.session
        r = requests.post(URL, headers=h, json=payload, timeout=180)
        sid = r.headers.get("Mcp-Session-Id")
        if sid:
            self.session = sid
        if r.status_code == 202:
            return None
        events = parse_sse_events(r)
        if want_id is not None:
            for ev in events:
                if ev.get("id") == want_id:
                    return ev
        return events[0] if events else None

    def _init(self):
        resp = self._post({
            "jsonrpc": "2.0", "id": str(uuid.uuid4()), "method": "initialize",
            "params": {"protocolVersion": "2025-03-26", "capabilities": {},
                       "clientInfo": {"name": "kimi-cli", "version": "1.0"}},
        })
        self._post({"jsonrpc": "2.0", "method": "notifications/initialized"})
        return resp

    def call(self, tool, args):
        last = None
        for attempt in range(10):
            req_id = str(uuid.uuid4())
            resp = self._post({
                "jsonrpc": "2.0", "id": req_id, "method": "tools/call",
                "params": {"name": tool, "arguments": args},
            }, want_id=req_id)
            last = resp
            if resp is None:
                time.sleep(3)
                continue
            sc = resp.get("result", {}).get("structuredContent") or {}
            if "no_unity_session" in json.dumps(sc):
                time.sleep(3)
                continue
            return resp
        return last

    def list_tools(self):
        resp = self._post({"jsonrpc": "2.0", "id": str(uuid.uuid4()),
                           "method": "tools/list", "params": {}})
        return resp


def main():
    cmd = sys.argv[1]
    m = MCP()
    if cmd == "list-tools":
        resp = m.list_tools()
        tools = resp.get("result", {}).get("tools", [])
        print("\n".join(t["name"] for t in tools))
    elif cmd == "call":
        tool = sys.argv[2]
        args = json.loads(sys.argv[3]) if len(sys.argv) > 3 else {}
        resp = m.call(tool, args)
        print(json.dumps(resp, ensure_ascii=False, indent=2)[:8000])
    elif cmd == "menu":
        # 菜单执行不可重试（非幂等），只发一次
        resp = m._post({
            "jsonrpc": "2.0", "id": str(uuid.uuid4()), "method": "tools/call",
            "params": {"name": "execute_menu_item", "arguments": {"menu_path": sys.argv[2]}},
        })
        print(json.dumps(resp, ensure_ascii=False, indent=2)[:8000])
    elif cmd == "resource":
        req_id = str(uuid.uuid4())
        resp = m._post({
            "jsonrpc": "2.0", "id": req_id, "method": "resources/read",
            "params": {"uri": sys.argv[2]},
        }, want_id=req_id)
        print(json.dumps(resp, ensure_ascii=False, indent=2)[:8000])


if __name__ == "__main__":
    main()
