pipeline {
    agent any

    environment {
        DOCKER_COMPOSE = 'docker-compose'
        WORKSPACE_DIR = "${WORKSPACE}"
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build Docker Images') {
            steps {
                dir("${WORKSPACE}") {
                    sh '''
                        docker-compose build message-sender message-receiver
                    '''
                }
            }
        }

        stage('Stop Existing Containers') {
            steps {
                dir("${WORKSPACE}") {
                    sh '''
                        docker-compose stop rabbitmq message-sender message-receiver prometheus grafana || true
                        docker-compose rm -f rabbitmq message-sender message-receiver prometheus grafana || true
                    '''
                }
            }
        }

        stage('Start Services') {
            steps {
                dir("${WORKSPACE}") {
                    sh '''
                        # Debug: show current directory and files
                        echo "Current directory: $(pwd)"
                        echo "Workspace: ${WORKSPACE}"
                        echo "Checking prometheus directory:"
                        ls -la prometheus/ || echo "prometheus directory not found"
                        
                        # Verify prometheus config file exists
                        if [ ! -f prometheus/prometheus.yml ]; then
                            echo "ERROR: prometheus/prometheus.yml not found in $(pwd)"
                            echo "Files in current directory:"
                            ls -la
                            exit 1
                        fi
                        
                        # Get absolute path for bind mount
                        ABS_CONFIG_PATH=$(realpath prometheus/prometheus.yml 2>/dev/null || echo "${WORKSPACE}/prometheus/prometheus.yml")
                        echo "Prometheus config file found: ${ABS_CONFIG_PATH}"
                        
                        # Export absolute path for docker-compose
                        export PROMETHEUS_CONFIG_PATH="${ABS_CONFIG_PATH}"
                        
                        # Start all services
                        # Use absolute path via environment variable
                        docker-compose up -d rabbitmq message-sender message-receiver prometheus grafana
                        
                        # Wait for services to start
                        sleep 5
                        
                        # Check if Prometheus started successfully
                        if docker ps | grep -q prometheus; then
                            echo "Prometheus container is running"
                            echo "--- Verifying Prometheus config ---"
                            docker exec prometheus cat /etc/prometheus/prometheus.yml | head -30 || echo "Could not read config from container"
                        else
                            echo "ERROR: Prometheus container is not running!"
                            echo "Checking logs:"
                            docker-compose logs prometheus | tail -20
                            exit 1
                        fi
                    '''
                }
            }
        }
    }

    post {
        always {
            echo 'Pipeline completed'
        }
        success {
            echo 'Pipeline succeeded!'
        }
        failure {
            echo 'Pipeline failed!'
            dir("${WORKSPACE}") {
                sh 'docker-compose logs'
            }
        }
    }
}

