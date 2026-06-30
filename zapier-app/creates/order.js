const perform = async (z, bundle) => {
  const backendBaseUrl = bundle.inputData.BackendBaseUrl.replace(/\/$/, '');
  const tokenResponse = await z.request({
    method: 'POST',
    url: `${backendBaseUrl}/api/auth/token`,
    headers: {
      'Content-Type': 'application/json',
    },
    body: {
      clientId: bundle.inputData.ClientId,
      clientSecret: bundle.inputData.ClientSecret,
      grantType: 'client_credentials',
    },
  });

  if (tokenResponse.status < 200 || tokenResponse.status >= 300) {
    throw new z.errors.Error(
      `Token request failed with status ${tokenResponse.status}`,
      'TokenRequestFailed',
      tokenResponse.status
    );
  }

  const accessToken = tokenResponse.json.accessToken;

  const response = await z.request({
    method: 'POST',
    url: `${backendBaseUrl}/api/orders`,
    headers: {
      Authorization: `Bearer ${accessToken}`,
      'Content-Type': 'application/json',
    },
    body: {
      OrderId: bundle.inputData.OrderId,
      CustomerEmail: bundle.inputData.CustomerEmail,
      TotalAmount: bundle.inputData.TotalAmount,
    },
  });

  if (response.status < 200 || response.status >= 300) {
    throw new z.errors.Error(
      `Backend request failed with status ${response.status}`,
      'BackendRequestFailed',
      response.status
    );
  }

  return response.json;
};

module.exports = {
  key: 'order',
  noun: 'Order',
  display: {
    label: 'Send Order to Backend',
    description: 'Sends an order payload to the ASP.NET Core backend.',
  },
  operation: {
    cleanInputData: false,
    inputFields: [
      {
        key: 'BackendBaseUrl',
        label: 'Backend Base URL',
        required: true,
        type: 'string',
        helpText: 'Enter your public backend base URL without a trailing slash.',
      },
      {
        key: 'ClientId',
        label: 'Client ID',
        required: true,
        type: 'string',
      },
      {
        key: 'ClientSecret',
        label: 'Client Secret',
        required: true,
        type: 'password',
      },
      {
        key: 'OrderId',
        label: 'Order ID',
        required: true,
        type: 'string',
      },
      {
        key: 'CustomerEmail',
        label: 'Customer Email',
        required: true,
        type: 'string',
      },
      {
        key: 'TotalAmount',
        label: 'Total Amount',
        required: true,
        type: 'number',
      },
    ],
    perform,
    sample: {
      BackendBaseUrl: 'https://your-ngrok-url.ngrok-free.app',
      ClientId: 'Zapier_App_01',
      OrderId: '12345',
      CustomerEmail: 'test@test.com',
      TotalAmount: 50.0,
    },
  },
};
