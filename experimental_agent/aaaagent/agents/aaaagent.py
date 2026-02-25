from pathlib import Path
from langchain.agents import create_agent
from langchain_mcp_adapters.client import MultiServerMCPClient
from langchain.messages import SystemMessage, AIMessage, HumanMessage, ToolMessage

from .schema import AAAAgentConfig, AgentType
from .prompt import get_system_prompt

# mcp files
from ..mcps.schema import MCPS
from ..mcps.mcp_main import get_mcps_configs

from rich.console import Console
from rich.style import Style
from rich.color import Color
from logging import Logger

console = Console()
logger = Logger(name="AAAAgent")


class AAAAgent():
    """
    A Simple Agent Interface that can take respective tools and prompts and runs the query.

    TODO: add chat & session interface support
    """

    def __init__(self, agent_config: AAAAgentConfig) -> None:
        self.agent_type = agent_config.agent_type
        self.workspace_path = agent_config.workspace_path
        self.system_prompt = get_system_prompt(AgentType.CoderAgent)

        # setup mcp client
        self.client = MultiServerMCPClient(get_mcps_configs([MCPS.Terminal, MCPS.HITL]))

    async def _get_tools(self):
        return await self.client.get_tools()

    async def invoke(self, contents: list):
        self.tools = await self._get_tools()
        self.agent = create_agent("google_genai:gemini-2.5-pro", self.tools)
        res = await self.agent.ainvoke(
            input={"messages": [SystemMessage(self.system_prompt), *contents]}
        )
        return res

    async def stream_invoke(self, contents: list):
        """
        Simple Terminal UI to stream LLM Response

        Doesnt Return Anything
        """
        self.tools = await self._get_tools()
        self.agent = create_agent("google_genai:gemini-2.5-pro", self.tools)
        logger.info("Starting Agent of type: ", self.agent_type)
        contents.insert(
            0, HumanMessage(f"\nMy working directory is: {self.workspace_path}\n")
        )

        ai_messages: list[AIMessage | HumanMessage | ToolMessage] = []
        async for message_chunk, metadata in self.agent.astream(
            input={"messages": [SystemMessage(self.system_prompt), *contents]},
            stream_mode="messages",
            config={"configurable": {"thread_id": "thread-1"}},
        ):
            if isinstance(message_chunk, AIMessage):
                if message_chunk.additional_kwargs:
                    function_call = message_chunk.additional_kwargs.get("function_call")
                    if function_call:
                        function_name = function_call.get("name")
                        if function_name:
                            console.print(
                                f"[italic]\nCalling {function_name}....",
                                style=Style(color=Color.from_rgb(144, 213, 255)),
                            )
                if message_chunk.content:
                    if isinstance(message_chunk.content, list):
                        for message_chunk_inst in message_chunk.content:
                            if isinstance(message_chunk_inst, dict):
                                if message_chunk_inst.get("text"):
                                    ai_messages.append(
                                        AIMessage(message_chunk_inst["text"])
                                    )
                                    console.print(
                                        f"[green]{message_chunk_inst['text']}", end=""
                                    )
                            else:
                                if message_chunk_inst.strip():
                                    ai_messages.append(AIMessage(message_chunk_inst))
                                    console.print(
                                        f"[green]{message_chunk_inst}", end=""
                                    )
                    elif isinstance(message_chunk.content, str):
                        if message_chunk.content.strip():
                            ai_messages.append(AIMessage(message_chunk.content))
                            console.print(f"[green]{message_chunk.content}", end="")

            if isinstance(message_chunk, ToolMessage):
                # TODO: is there anyway we can identify tool errors so that we can skip them in the chat history
                ai_messages.append(message_chunk)
                console.print(
                    f"[small] Tool Result: {message_chunk.content}",
                    style=Style(color=Color.from_rgb(173, 216, 230)),
                )

