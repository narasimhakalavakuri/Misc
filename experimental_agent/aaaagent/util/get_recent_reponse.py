from langchain_core.messages.base import BaseMessage
from typing import Literal

def get_recent_reponse(messages, message_type: type[BaseMessage]) -> BaseMessage | Literal['']:

    for i in messages[::-1]:
        if isinstance(i, message_type):
            return i 
    return ""

