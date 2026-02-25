import os
from pathlib import Path
from mcp.types import TextContent
from aaaagent import AgentType, AAAAgent, AAAAgentConfig
from mcp.server.fastmcp import FastMCP
from dotenv import load_dotenv
load_dotenv()


# Set up the MCP server
mcp = FastMCP("Code Review Agent")


# Define the tool to review code
@mcp.tool()
async def review_code(workspace_path: str, query: str | None):
    """
    Tool to perform code review using a Code Review Agent.
    You can pass a workspace path to be reviewed and an optional query (any instructions) to the tool and it will review.
    """

    agent = AAAAgent(AAAAgentConfig(
        workspace_path=Path(workspace_path),
        agent_type=AgentType.CoderAgent,
    ))

    # Perform code review using the agent
    response = await agent.invoke(contents=[query] if query else [])

    try:
        messages = response.get('messages', [])
        if not messages:
            return TextContent(type="text", text="No response generated.")

        last_message = messages[-1]
        
        if isinstance(last_message.content, list):
            final_text = last_message.content[0].get('text', str(last_message.content))
        else:
            final_text = last_message.content

        return TextContent(type="text", text=final_text)

    except Exception as e:
        return TextContent(type="text", text=f"Error parsing response: {str(e)}")
    
if __name__ == "__main__":
    mcp.run()  # Start the MCP server
