import uvicorn
from fastapi import FastAPI
from opentelemetry.instrumentation.fastapi import FastAPIInstrumentor
from app.routers import resolve, status

from opentelemetry import trace
from opentelemetry.sdk.trace import TracerProvider
from opentelemetry.sdk.trace.export import (
    BatchSpanProcessor,
    ConsoleSpanExporter,
)

provider = TracerProvider()
processor = BatchSpanProcessor(ConsoleSpanExporter())
provider.add_span_processor(processor)

# Sets the global default tracer provider
trace.set_tracer_provider(provider)

# Creates a tracer from the global tracer provider
tracer = trace.get_tracer("FoundationaLLM.DataSourceAPI")

from opentelemetry.exporter.jaeger.thrift import JaegerExporter

# create a JaegerExporter
jaeger_exporter = JaegerExporter(
    # configure agent
    agent_host_name='localhost',
    agent_port=6831,
    # optional: configure also collector
    # collector_endpoint='http://localhost:14268/api/traces?format=jaeger.thrift',
    # username=xxxx, # optional
    # password=xxxx, # optional
    # max_tag_value_length=None # optional
)

app = FastAPI(
    title='FoundationaLLM DataSourceHubAPI',
    summary='API for retrieving DataSource metadata',
    description='The FoundationaLLM DataSourceHubAPI is a wrapper around DataSourceHub functionality contained in the foundationallm.core Python SDK.',
    version='1.0.0',
    contact={
        'name':'Solliance, Inc.',
        'email':'contact@solliance.net',
        'url':'https://solliance.net/' 
    },
    openapi_url='/swagger/v1/swagger.json',
    docs_url='/swagger',
    redoc_url=None,
    license_info={
        "name": "FoundationaLLM Software License",
        "url": "https://www.foundationallm.ai/license",
    }
)

FastAPIInstrumentor.instrument_app(app)

app.include_router(resolve.router)
app.include_router(status.router)

@app.get('/')
async def root():
    """
    Root path of the API.
    
    Returns
    -------
    str
        Returns a JSON object containing a message and value.
    """
    return { 'message': 'FoundationaLLM DataSourceHubAPI' }
    
if __name__ == '__main__':
    uvicorn.run('main:app', host='0.0.0.0', port=8842, reload=True, forwarded_allow_ips='*', proxy_headers=True)