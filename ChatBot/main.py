from fastapi import FastAPI
from services.chat_bot import rag_pdf_qa
from dotenv import load_dotenv
import asyncio
import os

load_dotenv()
pdf_file = os.getenv("PDF_FILE_PATH")
model_file = os.getenv("MODEL_FILE_PATH")
SS_model_file = "paraphrase-multilingual-mpnet-base-v2"
query = "does textinger a secure chat app?"

app = FastAPI()

@app.get("/")
async def root():
    result = await asyncio.to_thread(
        rag_pdf_qa, pdf_file, model_file, "faiss_index", SS_model_file, query
    )
    return { "message": result }