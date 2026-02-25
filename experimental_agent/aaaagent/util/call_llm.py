import os
import base64
from pathlib import Path
from langchain_anthropic import ChatAnthropic
from langchain_google_genai import ChatGoogleGenerativeAI
from langchain_openai import ChatOpenAI


from dotenv import load_dotenv
load_dotenv()

client=genai.Client(api_key=os.environ.get('GOOGLE_API_KEY'))

def load_image(imgpath: Path):
    with open(imgpath,'rb') as f:
        return base64.b64encode(f.read()).decode('utf-8')

def gen(prompt: str, img_urls: Path | list[Path], llm_response_path: Path) -> str:
  """
  LLM Callers, which takes prompt and img urls (valid image paths).

  Stores the LLM output in `out/llm_output.txt` and returns the path of the output.
  """
  img_content = []
  if isinstance(img_urls, list):
    if len(img_urls):
       for img in img_urls:
          if isinstance(img, (str, Path)):
             img_content.append(genai.types.Part.from_bytes(
                data=load_image(img),
                mime_type='image/png'
             ))
          else:
             raise Exception(f"Unsupported Type recieved in list: {type(img)}")
       
  elif isinstance(img_urls, (str, Path)):
     img_urls = Path(img_urls)
     img_content.append(genai.types.Part.from_bytes(
                data=load_image(img_urls),
                mime_type='image/png'
      ))
  else: 
     raise Exception(f"Invalid Type of img_url: {type(img_urls)}")
     
     
     
     
  print('Generating... (check prompt at: \'out/llm_prompt.txt\')')
  open('out/llm_prompt.txt', 'w', encoding="utf8").write(prompt)
  response = client.models.generate_content(
      model='gemini-2.5-flash',
      contents=[
        *img_content,
        prompt]
    )
  print("Generation DONE. Trying to store the response at: ", llm_response_path)
  try:
    llm_response_path.resolve().parent.mkdir(exist_ok=True, parents=True)
    if response.text:
      with open(llm_response_path,'w', encoding='utf-8', errors='ignore') as f:
        f.write(response.text)
      print("LLM Response stored succesfully")

  except Exception as e: 
     print("Failed to store in default out/llm_response path: ", e)
     print("Storing output in cwd...")
     open(llm_response_path.parts[-1])

  return str(response.text)