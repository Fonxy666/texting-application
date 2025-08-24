from fastapi import FastAPI
from pydantic import BaseModel
from services.chat_bot import rag_pdf_qa
from dotenv import load_dotenv
import asyncio
import os
from fastapi.middleware.cors import CORSMiddleware

load_dotenv()
pdf_file = os.getenv("PDF_FILE_PATH")
model_file = os.getenv("MODEL_FILE_PATH")
SS_model_file = "paraphrase-multilingual-mpnet-base-v2"
query = "does textinger a secure chat app?"

origins = [
    "http://localhost:4200",
]

app = FastAPI()

app.add_middleware(
    CORSMiddleware,
    allow_origins=origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

class Message(BaseModel):
    text: str

@app.post("/ai-chat")
async def send_message(msg: Message):
    result = await asyncio.to_thread(
        rag_pdf_qa, pdf_file, model_file, "faiss_index", SS_model_file, msg.text
    )
    print(result)
    return { "message": result }