from dataclasses import dataclass
from enum import Enum
from pathlib import Path


class AgentType(Enum):
    CodeReviewAgent = "CodeReviewAgent"
    CoderAgent = "CoderAgent"


@dataclass
class AAAAgentConfig():
    workspace_path: Path
    agent_type: AgentType = AgentType.CoderAgent