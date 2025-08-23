import os.path
from langchain_huggingface import HuggingFaceEmbeddings
from langchain_community.vectorstores import FAISS
from langchain.chains.retrieval_qa.base import RetrievalQA
from langchain.memory import ConversationBufferMemory
from .pdf_extracter import extract_text_from_pdf
from models.custom_llm import custom_llm

def rag_pdf_qa(pdf_path, LLM_model_path, db_path, SS_model, query):
    text = extract_text_from_pdf(pdf_path)

    embeddings = HuggingFaceEmbeddings( model_name=SS_model )
        
    if os.path.exists(db_path):
        db = FAISS.load_local(db_path, embeddings, allow_dangerous_deserialization=True)

    else:
        db = FAISS.from_texts(text, embeddings)
        db.save_local(folder_path=db_path)

    llm = custom_llm(model=LLM_model_path, verbose=False)
    memory = ConversationBufferMemory(memory_key="chat_history", input_key="query")
    qa = RetrievalQA.from_chain_type(
        llm=llm,
        memory=memory,
        chain_type="stuff",
        retriever=db.as_retriever(),
        return_source_documents=False
        )
    result = qa.run(query)
    return result