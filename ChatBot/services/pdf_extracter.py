import fitz

def extract_text_from_pdf(pdf_path):
    text = ""
    try:
        doc = fitz.open(pdf_path)
        for page in doc:
            text += page.get_text()
        doc.close()
        
        return text
    except Exception as e:
        print("ERROR OCCURED: ",e)
        return None