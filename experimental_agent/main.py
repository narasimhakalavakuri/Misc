import os
import asyncio
from pathlib import Path
from rich.console import Console
from dotenv import load_dotenv

from langchain_core.messages import HumanMessage, AIMessage

from aaaagent import AAAAgent, AgentType, AAAAgentConfig


load_dotenv()
console = Console()


workspace_path = os.path.dirname(Path(__file__))
workspace_path = Path(r"C:\Users\Narasimha\Work\modernization")


agent = AAAAgent(AAAAgentConfig(
    workspace_path=Path(workspace_path),
    agent_type=AgentType.CodeReviewAgent,
))

query = """
Can you do a basic review of what the project is, and explain it.
"""


async def stream_agent():
    await agent.stream_invoke(contents=[HumanMessage(query)] if query else [])

asyncio.run(stream_agent())
