from fastapi import APIRouter, Depends
from app.dependencies import validate_api_key_header
from foundationallm.hubs.agent import AgentHub, AgentHubRequest, AgentHubResponse

router = APIRouter(
    prefix='/resolve_request',
    tags=['resolve_request'],
    dependencies=[Depends(validate_api_key_header)],
    responses={404: {'description':'Not found'}}
)

@router.post('/')
async def resolve_request(request: AgentHubRequest) -> AgentHubResponse:    
    return AgentHub().resolve_request(request)