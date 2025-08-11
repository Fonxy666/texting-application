export type ResponseSuccess<T> = T extends string ?
{
    isSuccess: true;
    message: T;
} : {
    isSuccess: true;
    data: T
}
  
export interface ResponseFailure {
    isSuccess: false;
    error?: {
        message: string
    }
}
  
export type ServerResponse<T> = ResponseSuccess<T> | ResponseFailure;