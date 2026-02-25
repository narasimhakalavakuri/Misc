from .prompts.code_review_prompt import CODE_REVIEW_SYSTEM_PROMPT_COMPLETE
from .prompts.coder_prompt import CODER_PROMPT

from .schema import AgentType

def get_system_prompt(agent_type: AgentType) -> str:
    """
    Returns System prompt for given agent type
    """

    match agent_type:

        case AgentType.CoderAgent:
            return CODER_PROMPT
        
        case AgentType.CodeReviewAgent:
            return CODE_REVIEW_SYSTEM_PROMPT_COMPLETE

        case _:
            return "You are an AI Assistant, help with users request."

