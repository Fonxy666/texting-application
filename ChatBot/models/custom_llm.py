from langchain_community.llms import GPT4All
from langchain.llms.base import LLM
from typing import List, Optional

class custom_llm(LLM):
    model: str
    verbose: bool = False
    temperature: float = 0.3
    top_p: float = 0.3
    top_k: int = 40
    max_tokens: int = 50
    device: str = "gpu"

    @property
    def _llm_type(self) -> str:
        return "custom_llm"
    
    def _call(self, prompt: str, stop: Optional[List[str]] = None) -> str:
        llm = GPT4All( model=self.model, verbose=self.verbose, device = self.device )
        response = llm.generate( [prompt], temp=self.temperature, top_p=self.top_p, top_k=self.top_k, max_tokens=self.max_tokens )
        return response.generations[0][0].text