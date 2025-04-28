export interface ResponseSuccess<T> {
    isSuccess: true;
    data: T;
}
  
export interface ResponseFailure {
    isSuccess: false;
    message?: string;
}
  
export type Response<T> = ResponseSuccess<T> | ResponseFailure;